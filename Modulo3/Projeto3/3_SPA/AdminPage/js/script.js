const apiUrl = "http://localhost:5164/";

document.addEventListener("DOMContentLoaded", function () {
	if (!sessionStorage.getItem("userData"))
		document.getElementById("login-modal-div").style.display = "flex";
});

document.body.addEventListener("submit", function (event) {
	event.preventDefault();
	if (event.target.id === "login-form")
		login(event.target.elements);
	else if (event.target.id === "form-modal-form")
		submitForm(event.target.elements);
});