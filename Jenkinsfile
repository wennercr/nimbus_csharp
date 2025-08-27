pipeline {
    agent any

    environment {
        GRID_URL = 'http://selenium-hub:4444'
        REMOTE   = 'true' // Always use Selenium Grid
        ALLURE_RESULTS_DIR = "${WORKSPACE}/allure-results"
    }

    stages {
        stage('Clean Workspace') {
            steps {
                cleanWs()
            }
        }

        stage('Checkout') {
            steps {
                git branch: 'main',
                    credentialsId: 'github-ssh',
                    url: 'git@github.com:wennercr/nimbus_csharp.git'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet restore'
                sh 'dotnet build --configuration Release --no-restore'
            }
        }

        stage('Set Selenium Properties') {
            steps {
                script {
                    def userInput = input(
                        id: 'seleniumInput',
                        message: 'Enter test configuration:',
                        parameters: [
                            string(name: 'Suite_Name', defaultValue: 'SampleSmoke', description: 'Test suite name'),
                            choice(name: 'Browser', choices: ['chrome', 'edge'], description: 'Browser to use'),
                            choice(name: 'Headless', choices: ['true', 'false'], description: 'Run in headless mode?'),
                            string(name: 'Groups', defaultValue: '', description: 'Comma-separated test groups (optional)'),
                            string(name: 'Threads', defaultValue: '1', description: '# of parallel threads')
                        ]
                    )

                    env.USER_SUITE_NAME = userInput['Suite_Name']
                    env.USER_BROWSER    = userInput['Browser']
                    env.USER_HEADLESS   = userInput['Headless']
                    env.USER_GROUPS     = userInput['Groups']
                    env.USER_THREADS    = userInput['Threads']
                }
            }
        }    

        stage('Prepare Runsettings') {
            steps {
                // If your Jenkins agent is Windows with Windows PowerShell:
                pwsh '''
                $ErrorActionPreference = "Stop"

                # Build OR filter like: TestCategory=smoke|TestCategory=login
                $raw = "$env:GROUPS".Replace(" ", "")
                $parts = $raw -split "," | Where-Object { $_ -ne "" }
                $filter = ($parts | ForEach-Object { "TestCategory=$_" }) -join "|"

                $runsetPath = "nimbus.runsettings"
                if (-not (Test-Path $runsetPath)) { throw "Runsettings not found at $runsetPath" }

                # Load XML
                [xml]$doc = Get-Content -Raw $runsetPath

                # Ensure nodes
                $runSettings = $doc.RunSettings
                if (-not $runSettings) { $runSettings = $doc.CreateElement("RunSettings"); $null = $doc.AppendChild($runSettings) }

                $rc = $runSettings.RunConfiguration
                if (-not $rc) { $rc = $doc.CreateElement("RunConfiguration"); $null = $runSettings.AppendChild($rc) }

                $trp = $runSettings.TestRunParameters
                if (-not $trp) { $trp = $doc.CreateElement("TestRunParameters"); $null = $runSettings.AppendChild($trp) }

                $nunit = $runSettings.NUnit
                if (-not $nunit) { $nunit = $doc.CreateElement("NUnit"); $null = $runSettings.AppendChild($nunit) }

                function Set-Param([System.Xml.XmlElement]$root, [string]$name, [string]$value) {
                $node = $root.SelectSingleNode("Parameter[@name='$name']")
                if (-not $node) {
                    $node = $root.OwnerDocument.CreateElement("Parameter")
                    $null = $node.SetAttribute("name", $name)
                    $null = $root.AppendChild($node)
                }
                $null = $node.SetAttribute("value", $value)
                }

                # Map pipeline vars to runsettings
                $suiteName = "$env:SUITE_NAME"
                $browser   = "$env:BROWSER"
                $headless  = "$env:HEADLESS".ToLower()
                $remote    = "$env:REMOTE"
                $gridUrl   = "$env:GRID_URL"
                $threads   = "$env:THREADS"

                Set-Param $trp "testSuiteName" $suiteName
                Set-Param $trp "browser"       $browser
                Set-Param $trp "headless"      $headless
                Set-Param $trp "remote"        $remote
                Set-Param $trp "gridUrl"       $gridUrl

                # Ensure NumberOfTestWorkers
                $workersNode = $nunit.SelectSingleNode("NumberOfTestWorkers")
                if (-not $workersNode) {
                $workersNode = $doc.CreateElement("NumberOfTestWorkers")
                $null = $nunit.AppendChild($workersNode)
                }
                $workersNode.InnerText = [string]$threads

                # Ensure/replace/remove TestCaseFilter
                $filterNode = $rc.SelectSingleNode("TestCaseFilter")
                if ($filter) {
                if (-not $filterNode) {
                    $filterNode = $doc.CreateElement("TestCaseFilter")
                    $null = $rc.AppendChild($filterNode)
                }
                $filterNode.InnerText = $filter
                Write-Host "Using TestCaseFilter: $filter"
                } else {
                if ($filterNode) { $null = $rc.RemoveChild($filterNode) }
                Write-Host "No groups provided; TestCaseFilter removed."
                }

                $doc.Save($runsetPath)

                # Ensure results dir exists
                $trxDir = "$env:TRX_DIR"
                New-Item -ItemType Directory -Force -Path $trxDir | Out-Null
                '''
            }
        }

    stage('Run Tests') {
      steps {
        catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
          script {
            def cmd = """
              dotnet test \\
                --configuration Release \\
                --no-build \\
                --logger "trx;LogFileName=test_results.trx" \\
                --results-directory "${env.TRX_DIR}" \\
                --settings nimbus.runsettings \\
                "${env.TEST_PROJ}"
            """.stripIndent().trim()
            echo "Final command:\\n${cmd}"
            sh cmd
          }
        }
      }
    }



       stage('Generate Allure Report') {
            steps {
                allure([
                    includeProperties: false,
                    jdk: '',
                    results: [[path: "**/allure-results"]],
                    reportBuildPolicy: 'ALWAYS'
                ])
            }
        }


    }
}