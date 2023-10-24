@echo off 
echo This will delete all goldens output files. Are you sure you wish to proceed?

pause

cd ../../FinModelUtility

set hierarchyListCmd="dir /b /s /ad *.* | sort"

for /d %%p in (*, Formats/*, Games/*) do ( 
  pushd "./"
  
  if exist %%p (
    cd "%%p"
  ) else (
    if exist "Formats/%%p" (
      cd "Formats/%%p"
    ) else (
      if exist "Games/%%p" (
        cd "Games/%%p"
      )
    )
  )
  
  if exist "%%p Tests" (
    cd "%%p Tests"
  )
    
  if exist goldens\ (
    for /f "tokens=*" %%d in ('%hierarchyListCmd%') do (
      pushd "./"

      cd "%%d"

      if exist input\ (
        if exist output\ (
          cd output\
          del /q *.*
        )
      )

      popd
    )
  )

  popd
)

pause