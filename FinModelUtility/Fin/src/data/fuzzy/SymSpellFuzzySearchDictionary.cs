﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace fin.data.fuzzy {
  public class SymSpellFuzzySearchDictionary<T> : IFuzzySearchDictionary<T> {
    /**
     * var maxEditDistance =
     * (int) Math.Floor(minMatchPercentage * .01 * this.impl_.MaxLength);
     */
    private const int MAX_EDIT_DISTANCE = 8;

    private readonly SymSpell impl_ =
        new(0,
            SymSpellFuzzySearchDictionary<T>.MAX_EDIT_DISTANCE,
            SymSpellFuzzySearchDictionary<T>.MAX_EDIT_DISTANCE + 1);

    private readonly IDictionary<string, ISet<T>> associatedData_ =
        new Dictionary<string, ISet<T>>();

    public void Add(string keyword, T associatedData) {
      foreach (var token in this.Tokenize_(keyword)) {
        this.AddToken_(token, associatedData);
      }
    }

    private void AddToken_(string token, T associatedData) {
      // TODO: Support token frequency?
      if (!this.associatedData_.TryGetValue(token, out var associatedDatas)) {
        associatedDatas = new HashSet<T>();
        this.associatedData_.Add(token, associatedDatas);

        this.impl_.CreateDictionaryEntry(token, 1);
      }

      associatedDatas.Add(associatedData);
    }

    public IEnumerable<IFuzzySearchResult<T>> Search(
        string filterText,
        float minMatchPercentage) {
      // TODO: Use minMatchPercentage.

      // 1) Split up keyword into tokens.
      var tokens = this.Tokenize_(filterText);
      var inverseTokenCount = 1f / tokens.Count();

      // TODO: Possible to do some of these lookups in O(1) time?
      var wipResults = new Dictionary<T, SymSpellFuzzySearchResult>();

      // 2) Look up each token in dictionary to find sets of data w/ that
      //    token.
      foreach (var token in tokens) {
        var matches = this.impl_.Lookup(token,
                                        SymSpell.Verbosity.Closest,
                                        MAX_EDIT_DISTANCE);

        // 3) Merge each match based on the data they map to.
        foreach (var match in matches) {
          var matchedKeyword = match.term;
          var matchPercentage = (1 -
                                 (1f * match.distance) /
                                 Math.Max(matchedKeyword.Length,
                                          filterText.Length)) *
                                100;

          foreach (var associatedData in this.associatedData_[matchedKeyword]) {
            if (!wipResults.TryGetValue(associatedData, out var result)) {
              result = new SymSpellFuzzySearchResult(associatedData);
              wipResults.Add(associatedData, result);
            }

            // 4) Divide each result's probability based on # of tokens.
            result.MatchPercentage += inverseTokenCount * matchPercentage;
          }
        }
      }

      return wipResults.Values;
    }

    private IEnumerable<string> Tokenize_(string keyword)
      => keyword.ToLower()
                .Split(' ', '_', '-', '/')
                .Select(rawToken => rawToken.Trim())
                .Where(token => !string.IsNullOrEmpty(token));

    private class SymSpellFuzzySearchResult : IFuzzySearchResult<T> {
      public T Data { get; }
      public float MatchPercentage { get; set; }

      public SymSpellFuzzySearchResult(T associatedData) {
        this.Data = associatedData;
      }
    }
  }
}