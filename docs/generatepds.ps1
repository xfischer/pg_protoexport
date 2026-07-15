Get-ChildItem -Path docs\examples\exports -Filter capture.tex -Recurse | ForEach-Object {
    Push-Location $_.Directory
    latexmk -pdf -interaction=nonstopmode -halt-on-error capture.tex
    latexmk -c capture.tex
    Pop-Location
}