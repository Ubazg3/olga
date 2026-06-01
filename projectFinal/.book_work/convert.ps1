$ErrorActionPreference = "Stop"
$docx = "C:\Users\idan9\projects\projectFinal\.book_work\out.docx"
$pdf  = "C:\Users\idan9\projects\projectFinal\.book_work\out.pdf"

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $doc = $word.Documents.Open($docx)
    foreach ($sr in $doc.StoryRanges) { $sr.Fields.Update() | Out-Null }
    foreach ($toc in $doc.TablesOfContents) { $toc.Update() }
    $doc.Save()
    $doc.ExportAsFixedFormat($pdf, 17)
    $pages = $doc.ComputeStatistics(2)
    Write-Output "PAGES: $pages"
    $doc.Close($false)
    Write-Output "OK"
} finally {
    $word.Quit()
}
