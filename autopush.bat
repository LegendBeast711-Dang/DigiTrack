 cd /d C:\Users\dml-admin\Downloads\Claude\DigiTrack
  xcopy /E /I /Y "%AppData%\DigiTrack\*" "sessions\"
  git add .
  git commit -m "Auto-save %date%"
  git push