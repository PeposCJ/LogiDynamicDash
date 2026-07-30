[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $OutputPath,

    [Parameter(Mandatory)]
    [string] $Version
)

$ErrorActionPreference = "Stop"

$document = [ordered]@{
    spdxVersion = "SPDX-2.3"
    dataLicense = "CC0-1.0"
    SPDXID = "SPDXRef-DOCUMENT"
    name = "LogiDynamicDash-$Version"
    documentNamespace =
        "https://github.com/PeposCJ/LogiDynamicDash/sbom/$Version"
    creationInfo = [ordered]@{
        created = (Get-Date).ToUniversalTime().ToString(
            "yyyy-MM-ddTHH:mm:ssZ")
        creators = @("Tool: LogiDynamicDash-New-PackageSbom.ps1")
    }
    packages = @(
        [ordered]@{
            name = "LogiDynamicDash"
            SPDXID = "SPDXRef-Package-LogiDynamicDash"
            versionInfo = $Version
            downloadLocation =
                "https://github.com/PeposCJ/LogiDynamicDash"
            filesAnalyzed = $false
            licenseConcluded = "MIT"
            licenseDeclared = "MIT"
            copyrightText = "NOASSERTION"
        },
        [ordered]@{
            name = "HidSharp"
            SPDXID = "SPDXRef-Package-HidSharp"
            versionInfo = "2.6.4"
            downloadLocation =
                "https://www.nuget.org/packages/HidSharp/2.6.4"
            filesAnalyzed = $false
            licenseConcluded = "Apache-2.0"
            licenseDeclared = "Apache-2.0"
            copyrightText = "NOASSERTION"
        },
        [ordered]@{
            name = "SVappsLAB.iRacingTelemetrySDK"
            SPDXID = "SPDXRef-Package-iRacingTelemetrySDK"
            versionInfo = "2.1.0"
            downloadLocation =
                "https://www.nuget.org/packages/SVappsLAB.iRacingTelemetrySDK/2.1.0"
            filesAnalyzed = $false
            licenseConcluded = "Apache-2.0"
            licenseDeclared = "Apache-2.0"
            copyrightText = "NOASSERTION"
        }
    )
    relationships = @(
        [ordered]@{
            spdxElementId = "SPDXRef-DOCUMENT"
            relationshipType = "DESCRIBES"
            relatedSpdxElement = "SPDXRef-Package-LogiDynamicDash"
        },
        [ordered]@{
            spdxElementId = "SPDXRef-Package-LogiDynamicDash"
            relationshipType = "DEPENDS_ON"
            relatedSpdxElement = "SPDXRef-Package-HidSharp"
        },
        [ordered]@{
            spdxElementId = "SPDXRef-Package-LogiDynamicDash"
            relationshipType = "DEPENDS_ON"
            relatedSpdxElement = "SPDXRef-Package-iRacingTelemetrySDK"
        }
    )
}

$document |
    ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $OutputPath -Encoding utf8
