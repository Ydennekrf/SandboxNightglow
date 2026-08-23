param()
$ErrorActionPreference = "Stop"
$itemPath = ".\Core\Inventory\Data\items_seed.csv"
$effectPath = ".\Core\Inventory\Data\item_effects_seed.csv"
$recipeDir = ".\Core\Crafting\Data"
$errors = New-Object System.Collections.Generic.List[string]

function Test-NoUtf8Bom {
    param(
        [string]$Path,
        [string]$Label
    )

    if (-not (Test-Path $Path)) {
        return
    }

    $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $Path))
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        $errors.Add("$Label starts with a UTF-8 BOM; Godot text resources should be UTF-8 without BOM")
    }
}

Test-NoUtf8Bom -Path $itemPath -Label "items_seed.csv"
Test-NoUtf8Bom -Path $effectPath -Label "item_effects_seed.csv"

Get-ChildItem -Path $recipeDir -Recurse -Include *.tres,*.res | ForEach-Object {
    Test-NoUtf8Bom -Path $_.FullName -Label $_.FullName
}

$items = Import-Csv $itemPath
$effects = Import-Csv $effectPath
$itemIds = @{}

function Test-CsvColumnCount {
    param(
        [string]$Path,
        [string]$Label
    )

    $lines = Get-Content $Path
    if ($lines.Count -eq 0) {
        $errors.Add("$Label is empty")
        return
    }

    $expected = ($lines[0].Split(",")).Count
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $actual = ($lines[$index] -split ",", -1).Count
        if ($actual -ne $expected) {
            $errors.Add("$Label line $($index + 1) has $actual columns; expected $expected")
        }
    }
}

Test-CsvColumnCount -Path $itemPath -Label "items_seed.csv"
Test-CsvColumnCount -Path $effectPath -Label "item_effects_seed.csv"

foreach ($item in $items) {
    $parsed = 0
    if (-not [int]::TryParse($item.id, [ref]$parsed)) {
        $errors.Add("Invalid item id '$($item.id)'")
        continue
    }

    if ($itemIds.ContainsKey($parsed)) {
        $errors.Add("Duplicate item id $parsed")
    } else {
        $itemIds[$parsed] = $item
    }
}

$itemNames = @{}
foreach ($item in $items) {
    if ([string]::IsNullOrWhiteSpace($item.name)) { continue }

    $nameKey = $item.name.Trim().ToLowerInvariant()
    if ($itemNames.ContainsKey($nameKey)) {
        $errors.Add("Duplicate item name '$($item.name)' for ids $($itemNames[$nameKey]) and $($item.id)")
    } else {
        $itemNames[$nameKey] = $item.id
    }
}

foreach ($item in $items) {
    if (-not [string]::Equals($item.category, "Weapon", [System.StringComparison]::OrdinalIgnoreCase)) {
        continue
    }

    if ([string]::IsNullOrWhiteSpace($item.combo_profile_path)) {
        continue
    }

    if (-not $item.combo_profile_path.StartsWith("res://")) {
        $errors.Add("Weapon item $($item.id) has non-res combo_profile_path '$($item.combo_profile_path)'")
        continue
    }

    $localComboPath = ".\" + $item.combo_profile_path.Substring(6).Replace("/", "\")
    if (-not (Test-Path $localComboPath)) {
        $errors.Add("Weapon item $($item.id) combo_profile_path is missing: $($item.combo_profile_path)")
    } else {
        Test-NoUtf8Bom -Path $localComboPath -Label $item.combo_profile_path
    }
}

$effectIndex = 1
foreach ($effect in $effects) {
    $effectIndex++
    $parsed = 0
    if (-not [int]::TryParse($effect.item_id, [ref]$parsed)) {
        $errors.Add("Invalid effect item_id '$($effect.item_id)' at row $effectIndex")
        continue
    }

    if (-not $itemIds.ContainsKey($parsed)) {
        $errors.Add("Effect row $effectIndex references missing item_id $($effect.item_id)")
    }
}

$recipeIds = @{}
Get-ChildItem -Path $recipeDir -Recurse -Include *.tres,*.res | ForEach-Object {
    $text = Get-Content $_.FullName -Raw
    if ($text -notmatch 'script_class="CraftingRecipe"') { return }

    $recipeId = [regex]::Match($text, 'RecipeId\s*=\s*"([^"]+)"').Groups[1].Value
    if ([string]::IsNullOrWhiteSpace($recipeId)) {
        $errors.Add("Recipe missing RecipeId: $($_.FullName)")
    } elseif ($recipeIds.ContainsKey($recipeId.ToLowerInvariant())) {
        $errors.Add("Duplicate RecipeId $recipeId in $($_.FullName)")
    } else {
        $recipeIds[$recipeId.ToLowerInvariant()] = $_.FullName
    }

    $resultText = [regex]::Match($text, 'ResultItemId\s*=\s*(\d+)').Groups[1].Value
    if ($resultText) {
        $resultId = [int]$resultText
        if (-not $itemIds.ContainsKey($resultId)) {
            $errors.Add("Recipe $recipeId result item $resultId is missing")
        }
    } else {
        $errors.Add("Recipe $recipeId missing ResultItemId")
    }

    foreach ($match in [regex]::Matches($text, 'ItemId\s*=\s*(\d+)')) {
        $ingredientId = [int]$match.Groups[1].Value
        if (-not $itemIds.ContainsKey($ingredientId)) {
            $errors.Add("Recipe $recipeId ingredient item $ingredientId is missing")
        }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Item seed validation passed. items=$($items.Count) effects=$($effects.Count) recipes=$($recipeIds.Count)"
