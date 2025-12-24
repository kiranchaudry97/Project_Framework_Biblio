# Fix non-ASCII characters in .resx value nodes and save as UTF8 without BOM
# This script removes diacritics (é -> e) and replaces smart punctuation (em dash, curly quotes) with ASCII equivalents.

$files = Get-ChildItem -Recurse -Filter *.resx
foreach ($f in $files) {
    try {
        $raw = Get-Content -Raw -Encoding UTF8 -ErrorAction Stop $f.FullName
        [xml]$doc = $raw
    } catch {
        try { $raw = Get-Content -Raw -Encoding Default $f.FullName; [xml]$doc = $raw } catch { Write-Host "Skip (parse failed): $($f.FullName) -> $($_.Exception.Message)"; continue }
    }

    $changed = $false
    if ($doc.root -and $doc.root.data) {
        foreach ($data in $doc.root.data) {
            if ($data.value -ne $null) {
                $text = $data.value.'#text'
                if ($text -ne $null) {
                    $new = $text

                    # replace em dash (U+2014) and en dash (U+2013) with hyphen
                    $new = $new -replace "`u2014", "-"
                    $new = $new -replace "`u2013", "-"

                    # replace curly quotes with straight quotes
                    $new = $new -replace "[`u2018`u2019]", "'"
                    $new = $new -replace "[`u201C`u201D]", '"'

                    # remove diacritics by decomposing and filtering NonSpacingMark
                    $norm = [string]$new
                    $norm = $norm.Normalize([System.Text.NormalizationForm]::FormD)
                    $sb = New-Object System.Text.StringBuilder
                    foreach ($ch in $norm.ToCharArray()) {
                        $uc = [Globalization.CharUnicodeInfo]::GetUnicodeCategory($ch)
                        if ($uc -ne [Globalization.UnicodeCategory]::NonSpacingMark) { [void]$sb.Append($ch) }
                    }
                    $deaccented = $sb.ToString().Normalize([System.Text.NormalizationForm]::FormC)

                    # also replace middle dot and degree sign if present
                    $deaccented = $deaccented -replace "`u00B7", "-"
                    $deaccented = $deaccented -replace "`u00B0", "deg"

                    if ($deaccented -ne $text) { $data.value.'#text' = $deaccented; $changed = $true }
                }
            }
        }
    }

    if ($changed) {
        try {
            $settings = New-Object System.Xml.XmlWriterSettings
            $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
            $settings.Indent = $true
            $writer = [System.Xml.XmlWriter]::Create($f.FullName, $settings)
            $doc.Save($writer)
            $writer.Close()
            Write-Host "Fixed: $($f.FullName)"
        } catch {
            Write-Host "Failed to write $($f.FullName): $($_.Exception.Message)"
        }
    }
}
Write-Host "Done."