document.addEventListener("DOMContentLoaded", function () {
    const baseURL = sessionStorage.getItem("baseURL");
    document.getElementById("baseURLID")?.setAttribute("value", baseURL);

    const emailInput = document.getElementById("emailInput");
    const passwordInput = document.getElementById("passwordInput");
    const loginAsOthersCheckbox = document.getElementById("loginAsOthersCheckbox");
    const emailAsContainer = document.getElementById("emailAsContainer");
    const emailAsInput = document.getElementById("emailAsInput");
    const loginForm = document.getElementById("loginForm");

    emailInput?.setAttribute("required", "required");
    passwordInput?.setAttribute("required", "required");

    loginAsOthersCheckbox?.addEventListener("change", function () {
        emailAsContainer.style.display = this.checked ? "block" : "none";
        emailAsInput?.toggleAttribute("required", this.checked);
    });

    loginForm?.addEventListener("submit", async function (event) {
        event.preventDefault();
        const formData = new FormData(this);

        try {
            const response = await fetch(this.action, {
                method: "POST",
                body: formData,
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            const data = await response.json();

            if (data.success) {
                console.log("Login successful");
                await getUserAccessData(data.email, baseURL);
            } else {
                console.log("Login failed");
            }
        } catch (error) {
            console.error("Error:", error);
        }
    });
});

async function getUserAccessData(email, baseURL) {
    console.log("Fetching user access data for:", email);

    Swal.fire({
        title: "Loading Page!",
        timerProgressBar: true,
        allowOutsideClick: false,
        didOpen: () => Swal.showLoading()
    });

    try {
        const response = await fetch(`${baseURL}/Home/GetUserAccessDataEmail?Email=${email}`);
        const data = await response.json();

        if (data.user_ID) {
            const sessionData = {
                User_ID: data.user_ID,
                UserName: data.userName,
                Employee_ID: data.employee_ID,
                NIKLoginID: data.nik,
                NIK: data.nik,
                Emp_Name: data.name,
                Emp_Email: data.email,
                Department: data.department,
                Job_Title: data.job_Title,
                Role_ID: data.role_ID,
                Role: data.role,
                Remarks: data.remarks,
                Status_ID: data.status_ID.toString(),
                Status: data.status,
                NIK_Manager: data.nik_Manager,
                Name_Manager: data.name_Manager,
                Email_Manager: data.email_Manager,
                NIK_PMBP: data.nik_PMBP,
                Name_PMBP: data.name_PMBP,
                Email_PMBP: data.email_PMBP,
                People_PMBP: data.people_PMBP?.toString(),
                People_Viewers: data.people_Viewers?.toString()
            };

            Object.entries(sessionData).forEach(([key, value]) => sessionStorage.setItem(key, value));
            location.href = `${baseURL}/Home/Index`;
        }
    } catch (error) {
        console.error("Failed to fetch user data:", error);
    }
}
