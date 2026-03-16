Write-Host "Running MyMediaVerse Frontend Tests..." -ForegroundColor Green
Write-Host ""

Write-Host "Installing dependencies if needed..." -ForegroundColor Yellow
npm install

Write-Host ""
Write-Host "Running tests..." -ForegroundColor Yellow
npm run test:run

Write-Host ""
Write-Host "Tests completed!" -ForegroundColor Green
Read-Host "Press Enter to continue"
