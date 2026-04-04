try {
    # --- CẤU HÌNH ---
    $sourcePath = "." 
    $outputFile = "Project_Code_Full_Context.txt"

    # Các đuôi file muốn gộp
    $includeExtensions = @(".cs", ".cshtml", ".css", ".js", ".json", ".csproj")

    # Các thư mục BỎ QUA
    $excludeFolders = @("bin", "obj", ".vs", ".git", ".idea", "lib", "node_modules", "migrations")

    # --- XỬ LÝ ---
    Write-Host "Dang chuan bi quet..." -ForegroundColor Cyan

    if (Test-Path $outputFile) { Remove-Item $outputFile }
    New-Item -Path $outputFile -ItemType File -Force | Out-Null

    $files = Get-ChildItem -Path $sourcePath -Recurse -File | Where-Object {
        $item = $_
        $isExcluded = $false
        foreach ($folder in $excludeFolders) {
            if ($item.FullName -match "\\$folder\\") { $isExcluded = $true; break }
        }
        (-not $isExcluded) -and ($includeExtensions -contains $item.Extension)
    }

    $totalFiles = $files.Count
    $count = 0

    if ($totalFiles -eq 0) {
        Write-Host "KHONG TIM THAY FILE NAO! Hay kiem tra lai duong dan." -ForegroundColor Red
    } else {
        foreach ($file in $files) {
            $count++
            $relativePath = $file.FullName.Substring((Get-Item $sourcePath).FullName.Length + 1)
            Write-Host "[$count/$totalFiles] Dang xu ly: $relativePath" -ForegroundColor Green
            
            # Dùng ngoặc đơn để tránh lỗi
            Add-Content -Path $outputFile -Value '=============================================================================='
            Add-Content -Path $outputFile -Value "FILE PATH: $relativePath"
            Add-Content -Path $outputFile -Value '=============================================================================='
            Add-Content -Path $outputFile -Value '```'
            
            try {
                $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
                Add-Content -Path $outputFile -Value $content
            } catch {
                # Đã sửa dòng lỗi ở đây (dùng ngoặc đơn)
                Add-Content -Path $outputFile -Value '[ERROR READING FILE]'
            }
            
            Add-Content -Path $outputFile -Value '```'
            Add-Content -Path $outputFile -Value ""
            Add-Content -Path $outputFile -Value ""
        }
        Write-Host "THANH CONG! File da duoc luu tai: $outputFile" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "CO LOI XAY RA:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
}

# --- Dừng màn hình để xem kết quả ---
Write-Host "------------------------------------------------"
Read-Host -Prompt "Nhan phim ENTER de thoat..."