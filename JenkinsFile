pipeline {
    agent any

    environment {
        GRID_URL = 'http://selenium-hub:4444'
        REMOTE   = 'true' // Always use Selenium Grid
    }

    stages {
        stage('Clean Workspace') {
            steps {
                cleanWs()
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

        stage('Checkout') {
            steps {
                git branch: 'main',
                    credentialsId: 'github-ssh',
                    url: 'git@github.com:wennercr/nimbus.git'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet restore'
                sh 'dotnet build --configuration Release --no-restore'
            }
        }

        stage('Run Tests') {
            steps {
                script {
                    // Start with the base dotnet test command
                    def cmd = """
                        dotnet test \
                          --configuration Release \
                          --no-build \
                          --logger "trx;LogFileName=test_results.trx" \
                          -- NUnit.NumberOfTestWorkers=${env.USER_THREADS} \
                          -- TestRunParameters.Parameter(name=testSuiteName,value=${env.USER_SUITE_NAME}) \
                          -- TestRunParameters.Parameter(name=browser,value=${env.USER_BROWSER}) \
                          -- TestRunParameters.Parameter(name=headless,value=${env.USER_HEADLESS}) \
                          -- TestRunParameters.Parameter(name=remote,value=${env.REMOTE}) \
                          -- TestRunParameters.Parameter(name=gridUrl,value=${env.GRID_URL})
                    """.stripIndent().trim()

                    // Add groups if provided
                    if (env.USER_GROUPS?.trim()) {
                        cmd += " -- TestRunParameters.Parameter(name=groups,value=${env.USER_GROUPS})"
                    }

                    echo "Final command: ${cmd}"
                    sh cmd
                }
            }
        }

        stage('Generate Allure Report') {
            steps {
                allure([
                    includeProperties: false,
                    jdk: '',
                    results: [[path: 'allure-results']],
                    reportBuildPolicy: 'ALWAYS'
                ])
            }
        }
    }
}