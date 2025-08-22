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
                          rm -rf "${ALLURE_RESULTS_DIR}" || true
                          mkdir -p "${ALLURE_RESULTS_DIR}"

                          ${cmd}

                          echo "Allure results directory: ${ALLURE_RESULTS_DIR}"
                          ls -la "${ALLURE_RESULTS_DIR}" || true
                        """
                    }
                }
            }
        }


        stage('Generate Allure Report') {
            steps {
                sh """
                  echo "Generating Allure report from ${ALLURE_RESULTS_DIR}"
                  allure generate -c -o "${WORKSPACE}/allure-report" "${ALLURE_RESULTS_DIR}"
                """
            }
        }
    }
}