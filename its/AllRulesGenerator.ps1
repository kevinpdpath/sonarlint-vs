﻿$ErrorActionPreference = "Stop"

$max = 10000
@"
<?xml version="1.0" encoding="utf-8"?>
<RuleSet Name="AllSonarLintRules" Description="Ruleset used to test for rule regressions." ToolsVersion="14.0">
<!-- This list is just hardcoded for now with plausible existing & upcoming rule IDs -->
<!-- It would be better to generate the actual list from the analyzer assemblies -->
  <Rules AnalyzerId="SonarLint.CSharp" RuleNamespace="SonarLint.CSharp">
"@
for ($i = 0; $i -le $max; $i++) { Write-Output "    <Rule Id=""S$i"" Action=""Warning"" />" }
@"
  </Rules>
  <Rules AnalyzerId="SonarLint.VisualBasic" RuleNamespace="SonarLint.VisualBasic">
"@
for ($i = 0; $i -le $max; $i++) { Write-Output "    <Rule Id=""S$i"" Action=""Warning"" />" }
@"
  </Rules>
</RuleSet>
"@
