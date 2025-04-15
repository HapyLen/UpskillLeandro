function login(inputs)
{
	if (document.getElementById("login-modal-div").style.display == "none")
		return ;
	document.getElementById("loading-modal-div").style.display = "flex";
	var options = {
		method: "POST",
		headers: {
			"Content-Type": "application/json",
			"Authorization": `Bearer ${sessionStorage.getItem("myToken")}`,
		},
		body: JSON.stringify({ email : inputs[0].value, password : inputs[1].value }),
	};
	fetch(apiUrl + "login", options).then((response) => {
		document.getElementById("loading-modal-div").style.display = "none";
		if (!response.ok) {
			alert("Error logging in. Please try again later.");
			throw new Error('Network response was not ok');
		}
		return response.json();
	}).then((data) => {
		if (data) {
			sessionStorage.setItem("myToken", data.token);
			document.getElementById("login-modal-div").style.display = "none";
		}
		else {
			document.getElementById("login-message").style.display = "flex";
		}
	}).catch((error) => {
		console.error("Error:", error);
	});
}

function togglePassword() {
	let input = document.getElementById("login-form-password-input")
	if (input.type == "password")
		input.type = "text";
	else
		input.type = "password";
}

function logout() {
	sessionStorage.removeItem('myToken');
	location.reload(true);
}