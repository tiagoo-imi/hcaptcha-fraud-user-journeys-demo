# User Journeys Demo (Azure Functions + Static Web Apps)

This repository contains a **local and deployable demo** showcasing **hCaptcha User Journeys and Fraud Protection** using:

- Azure Functions (HTTP triggers)
- Azure Table Storage
- Azure Static Web Apps (SWA)
- hCaptcha Enterprise (bot protection, fraud protection, and user journeys)

The goal of this demo is to show how **sessions, identities, and user actions** can be correlated across a full user journey (signup, login, cart, checkout, password reset, logout).

---

## Prerequisites

Make sure you have the following installed:

- **Node.js** (LTS recommended)
- **Azure Functions Core Tools**
- **Azure Static Web Apps CLI**
- **Azurite** (local Azure Storage emulator)

---

## 1. Install Azurite (Azure Storage Emulator)

Azurite is used to run Azure Table Storage locally.

### Option A – Install via npm (recommended)

```bash
npm install -g azurite
```

Run Azurite:

```bash
azurite
```

By default, it will expose:

- Blob: `127.0.0.1:10000`
- Queue: `127.0.0.1:10001`
- Table: `127.0.0.1:10002`

---

### Option B – Run via Docker

```bash
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

---

## 2. Install Azure Static Web Apps CLI (SWA)

The SWA CLI runs the frontend and Azure Functions together locally, simulating the Azure environment.

```bash
npm install -g @azure/static-web-apps-cli
```

Verify installation:

```bash
swa --version
```

---

## 3. Configuration (IMPORTANT)

This demo requires **both backend and frontend configuration** before running locally.

---

### 3.1 Backend configuration (Azure Functions)

For security reasons, the real Azure Functions settings file is **not committed** to the repository.

A template file is provided instead:

```
api/local.settings.json.example
```

#### Setup steps

After cloning the repository, create your local settings file:

```bash
cp api/local.settings.json.example api/local.settings.json
```

Then open `api/local.settings.json` and update the values with:

- `JwtSecret`
- hCaptcha **Enterprise Secret Key**
- Any other environment-specific values

⚠️ **Do not commit `local.settings.json` to GitHub.**

---

### 3.2 Frontend configuration (hCaptcha sitekey)

The frontend uses **static HTML** (no build step).

Because of this, the hCaptcha **sitekey must be configured explicitly**.

Update the hCaptcha sitekey used by the widget in the HTML pages.

Example:

```html
<div class="h-captcha" data-sitekey="YOUR_SITEKEY_HERE"></div>
```

Replace `YOUR_SITEKEY_HERE` with your **own hCaptcha sitekey**.

---

## 4. Run Locally

Start Azurite (if not already running):

```bash
azurite
```

From the project root, start the demo:

```bash
swa start .\frontend\ --api-location .\api\
```

This will:

- Serve the frontend
- Start Azure Functions
- Connect everything together locally

You should see URLs similar to:

- Frontend: `http://localhost:4280`
- Functions: `http://localhost:7071`

---

## Notes

- Cookies are configured with `Secure=false` for local HTTP development.
- In Azure / production, cookies **must** be `Secure=true` (HTTPS).
- The demo intentionally returns **hCaptcha evaluation data** in API responses for visualization and learning purposes.
- This behavior is **for demo purposes only** and must not be used in production systems.

---

## Disclaimer

This project is provided as a **demo and educational reference only**.

It is **not production-ready** and intentionally prioritizes clarity and observability over security hardening.
