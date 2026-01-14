import { signup, setAuthState } from "./auth.js";
import { showError } from "./ui.js";

function getValue(id) {
  return document.getElementById(id)?.value?.trim() || "";
}

window.onSignupSubmit = async function (token) {
  const email = getValue("email");
  const password = document.getElementById("password")?.value || "";
  const fullName = getValue("fullName");

  if (!email || !password) {
    showError("Email and password are required.");
    window.hcaptcha?.reset();
    return;
  }

  try {
    const res = await signup({
      email,
      password,
      fullName,
      hcaptchaToken: token
    });

    if (res.hcaptcha_response && window.HcaptchaDemoModal) {
      HcaptchaDemoModal.show(res.hcaptcha_response);
    }

    if (!res.ok) {
      showError(r.message || "Signup failed.");
      window.hcaptcha?.reset();
      return;
    }

    setAuthState({ email: res.email, fullName: res.full_name });
    window.location.href = "app.html";
  } catch (err) {
    showError(err?.message || "Signup failed.");
    window.hcaptcha?.reset();
  }
};


