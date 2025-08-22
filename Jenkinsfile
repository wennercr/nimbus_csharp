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

        stage('Run Tests') {
            steps {
                catchError(buildResult: 'UNSTABLE', stageResult: 'FAILURE') {
                    script {
                        def filterArg = (env.USER_GROUPS?.trim())
                            ? "--filter \"TestCategory=${env.USER_GROUPS}\""
                            : ""

                        def cmd = """
                            dotnet test \
                            --configuration Release \
                            --no-build \
                            --logger "trx;LogFileName=test_results.trx" \
                            -- NUnit.NumberOfTestWorkers=${env.USER_THREADS} \
                            -- "TestRunParameters.Parameter(name=\\"testSuiteName\\", value=\\"${env.USER_SUITE_NAME}\\")" \
                            -- "TestRunParameters.Parameter(name=\\"browser\\", value=\\"${env.USER_BROWSER}\\")" \
                            -- "TestRunParameters.Parameter(name=\\"headless\\", value=\\"${env.USER_HEADLESS}\\")" \
                            -- "TestRunParameters.Parameter(name=\\"remote\\", value=\\"${env.REMOTE}\\")" \
                            -- "TestRunParameters.Parameter(name=\\"gridUrl\\", value=\\"${env.GRID_URL}\\")" \
                            ${filterArg}
                        """.stripIndent().trim()

                        echo "Final command: ${cmd}"

                        sh """
                        set -e

                        # run tests (they will write to **/bin/.../target/allure-results per allureConfig.json + bootstrapper)
                        ${cmd}

                        # collect all allure-results into a central dir for Jenkins plugin
                        CENTRAL_DIR="${WORKSPACE}/allure-results"
                        rm -rf "$CENTRAL_DIR" || true
                        mkdir -p "$CENTRAL_DIR"

                        echo "Scanning for produced allure-results..."
                        FOUND_DIRS=\$(find "$WORKSPACE" -type d -path "*/target/allure-results" | sort -u)
                        echo "\$FOUND_DIRS" | sed '/^\\s*\$/d' || true

                        for d in \$FOUND_DIRS; do
                            echo "Sync: \$d -> \$CENTRAL_DIR"
                            cp -R "\$d/." "\$CENTRAL_DIR/" || true
                        done

                        echo "Centralized Allure results in: \$CENTRAL_DIR"
                        ls -la "\$CENTRAL_DIR" || true
                        """
                    }
                }
            }
        }


       stage('Generate Allure Report') {
            steps {
                allure([
                    includeProperties: false,
                    jdk: '',
                    results: [[path: "${WORKSPACE}/allure-results"]],
                    reportBuildPolicy: 'ALWAYS'
                ])
            }
        }


    }
}