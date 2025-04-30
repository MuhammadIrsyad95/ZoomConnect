document.addEventListener("DOMContentLoaded", async function () {
    try {
        const response = await fetch("/Home/GetUserAccessData");
        if (!response.ok) throw new Error("User not authenticated");

        const data = await response.json();
        console.log("User Data:", data);

        if (data && data.user_ID) {
            document.getElementById("navUserName").innerText = data.emp_Name || "User";
            document.getElementById("navUserRole").innerText = `${data.role || "Unknown"} | ${data.department || "No Dept"}`;
            document.getElementById("loginMenu").style.display = "none";
            document.getElementById("logoutMenu").style.display = "block";
        } else {
            throw new Error("User data is empty");
        }
    } catch (error) {
        console.warn("User not logged in:", error);
        document.getElementById("navUserName").innerText = "Welcome, Guest";
        document.getElementById("navUserRole").innerText = "";
        document.getElementById("loginMenu").style.display = "block";
        document.getElementById("logoutMenu").style.display = "none";
    }

    // Logout button functionality
    document.getElementById("logoutButton").addEventListener("click", function (e) {
        e.preventDefault();
        window.location.href = "/Account/Logout"; // Logout melalui backend
    });
});
