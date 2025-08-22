# Nimbus Test Automation Framework (.NET)

Nimbus is a lightweight, extensible test automation framework designed for web UI testing using **Selenium**, **NUnit**, **.NET 9.0**, and **Allure Reporting**.  
The framework is fully integrated with **Docker**, running against **Selenium Grid** and executing in **Jenkins** or **GitHub Actions** for CI/CD.

---

## 📦 Project Structure

```
nimbus-csharp/
│
├── docker-compose.yml          # Defines Jenkins, Selenium Grid Hub, Chrome Node
├── Dockerfile.jenkins          # Jenkins setup with .NET SDK + Allure CLI
├── src/
│   ├── Nimbus.Framework/       # Core framework code (DriverFactory, ConfigLoader, Logger, etc.)
│   └── Nimbus.Testing/         # Example NUnit tests
├── target/                     # Test results & Allure output (created at runtime)
├── config.properties           # Runtime config (browser, grid URL, etc.)
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

Install:

- [Docker](https://www.docker.com/products/docker-desktop)  
- [.NET SDK 9.0+](https://dotnet.microsoft.com/download)  
- (Optional) [Allure CLI](https://docs.qameta.io/allure/#_installing_a_commandline) for local report viewing  

---

## 🐳 Docker Setup

### Start Containers

To build and start all containers:

```bash
docker-compose up --build
```

If no changes to `Dockerfile.jenkins`, you can skip `--build`:

```bash
docker-compose up
```

Nimbus spins up:

- `selenium-hub` → Selenium Grid hub  
- `chrome` → Chrome node for execution  
- `jenkins` → Jenkins CI instance  

---

## 🛠 Jenkins Setup

### 1. Jenkins Dockerfile

Your Jenkins image is extended to install .NET and Allure:

```dockerfile
FROM jenkins/jenkins:lts

USER root
RUN apt-get update &&     apt-get install -y curl unzip openjdk-21-jdk dotnet-sdk-9.0
USER jenkins
```

### 2. Run Jenkins

After containers are up, go to:

```
http://localhost:8080
```

Unlock Jenkins, install suggested plugins, create an admin user, then create a **Pipeline job** pointing to your repo with the provided `Jenkinsfile`.

---

## ✅ Running the Framework

### Run Tests Locally

```bash
dotnet test   -- NUnit.NumberOfTestWorkers=2   -- TestRunParameters.Parameter(name="browser", value="chrome")   -- TestRunParameters.Parameter(name="headless", value="true")   -- TestRunParameters.Parameter(name="remote", value="true")   -- TestRunParameters.Parameter(name="gridUrl", value="http://localhost:4444")
```

### Run in Jenkins

Inside Jenkins/Docker network, use:

```
http://selenium-hub:4444
```

so Grid is resolved by container DNS.

---

## 📊 Viewing Allure Reports

Allure results are generated at:

```
target/allure-results
```

View locally:

```bash
allure serve target/allure-results
```

In Jenkins:

- Install the Allure Jenkins plugin  
- Point results path to: `target/allure-results`  
- Reports are always generated, even if tests fail  

In GitHub Actions:  
Reports are published to `gh-pages` automatically, versioned by run ID.

---

## 🧪 Test Configurations

### `config.properties`

Default config lives here:

```properties
browser=chrome
headless=true
remote=true
gridUrl=http://selenium-hub:4444
parallel.threads=1
```

Override with CLI params or CI/CD environment variables.

---

## ⚙️ Dynamic Test Inputs (CI)

Both Jenkins and GitHub Actions support dynamic test parameters:

- **Suite Name**  
- **Browser** (chrome, edge)  
- **Headless** (true/false)  
- **Groups** (NUnit categories)  
- **Threads** (parallel workers)  

These are passed into the framework using NUnit’s `TestRunParameters`.

---

## 🔍 Debugging Selenium Grid

Check Grid is up:

```bash
curl http://localhost:4444/status
```

Run a quick session:

```bash
curl -X POST http://localhost:4444/session -H "Content-Type: application/json" -d '{"capabilities":{"alwaysMatch":{"browserName":"chrome"}}}'
```

---

## 🧹 Cleanup

Shut down everything:

```bash
docker-compose down -v
```

---

## 📦 Version Snapshot

As of **Aug 2025**, Nimbus .NET runs with:

- Selenium Grid: 4.34.0  
- Chrome: 138.0  
- .NET: 9.0  
- NUnit: 4.x  
- Allure: CLI v2  

---

## 🧠 Notes

- Allure results always stored in `target/allure-results` (matches `allureConfig.json`).  
- Jenkins build continues as **UNSTABLE** when tests fail, ensuring Allure still runs.  
- GitHub Actions publishes reports under `/allure-report/latest`.  

---

## ⚡ GitHub Actions (CI/CD)

Nimbus also runs in **GitHub Actions** for continuous integration.  
Workflow file: `.github/workflows/test.yml`

### Triggered On
- Push to `main`  
- Pull requests to `main`  
- Manual `workflow_dispatch` with inputs  

### Environment Variables
Defined in `test.yml`:

- `SUITE_NAME` → Test suite name  
- `BROWSER` → Target browser  
- `HEADLESS` → Run in headless mode  
- `GROUPS` → NUnit categories (e.g. `smoke`)  
- `THREADS` → # of parallel workers  
- `GRID_URL` → Selenium Grid URL  
- `ALLURE_RESULTS_DIR` → `target/allure-results`  
- `ALLURE_REPORT_DIR` → `target/allure-report`  

### Steps
1. **Checkout repo**  
2. **Install .NET SDK**  
3. **Cache dependencies**  
4. **Prepare Allure directories**  
5. **Start Selenium Grid (Docker standalone Chrome)**  
6. **Install Allure CLI**  
7. **Run NUnit tests** (`dotnet test`)  
8. **Generate Allure report**  
9. **Upload as artifact**  
10. **Deploy report to GitHub Pages**  
11. **Post run + latest links in job summary**  

### Report Links
After each run, the summary shows:

- 🔗 **This run:** direct report for the specific run  
- 🔁 **Latest:** always points to the latest published report  
