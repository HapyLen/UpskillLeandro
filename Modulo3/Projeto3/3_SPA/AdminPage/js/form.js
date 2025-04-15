function showFormModal() {
	document.getElementById("form-modal-div").style.display = "flex";
	getMethods();
}

function buildForm() {
	const selector = document.getElementById('method-selector');
	const form = document.getElementById('form-modal-form');
	while (form.firstElementChild)
		form.removeChild(form.firstElementChild);
	let formStructure = methods[selector.value];
	if (formStructure)
	{
		formStructure.forEach(field => {
			const label = document.createElement('label');
			label.setAttribute('for', field.name);
			label.textContent = field.name;
			const input = document.createElement('input');
			input.setAttribute('id', field.name);
			input.setAttribute('type', field.type);
			if (field.size)
				input.setAttribute('size', field.size);
			if (field.mandatory) {
				input.required = true;
			}
			form.appendChild(label);
			form.appendChild(input);
			form.appendChild(document.createElement('br'))
		});
		const clearInput = document.createElement('button');
		clearInput.setAttribute('id', 'modal-form-clear-button');
		clearInput.setAttribute('type', 'reset');
		clearInput.setAttribute('value', 'Reset form');
		clearInput.textContent = "Clear";
		form.appendChild(clearInput);
		const submitButton = document.createElement('button');
		submitButton.setAttribute('id', 'modal-form-submit-button');
		submitButton.setAttribute('type', 'submit');
		submitButton.textContent = 'Submit';
		form.appendChild(submitButton);
	}
}

function submitForm(data) {
	let i = 0;
	const package = {};
	while (i < data.length - 2)
	{
		package[data[i].id] = data[i].value;
		i++;
	}
	const selector = document.getElementById('method-selector');
	var options = {
		method: "POST",
		headers: {
			"Content-Type": "application/json",
			"Authorization": `Bearer ${sessionStorage.getItem("myToken")}`,
		},
		body: JSON.stringify(package),
	};
	fetch(apiUrl + `ResolveMethod/${selector.value}`, options).then((response) => {
		if (!response.ok) {
			throw new Error('Network response was not ok');
		}
		return response.json();
	}).then((data) => {
		return ;
	}).catch((error) => {
		console.error("Error:", error);
	});
}
