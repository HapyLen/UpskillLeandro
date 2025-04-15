let currentTab;
let items;

function searchInput() {
	let input = document.getElementById("search-box-input").value;
	if (input == "")
		changeTab(currentTab, null, 1);
	else
		changeTab(currentTab, input, 1);
	document.getElementById("search-box-input").value = "";
}

function changeTab(tab, search, page) {
	document.getElementById("loading-modal-div").style.display = "flex";
	currentTab = tab;
	if (tab === "reservas")
		document.getElementById("tab-title").innerHTML = "Reservas";
	else if (tab === "obras")
		document.getElementById("tab-title").innerHTML = "Obras";
	else if (tab === "nucleos")
		document.getElementById("tab-title").innerHTML = "Núcleos";
	else if (tab === "utilizadores")
		document.getElementById("tab-title").innerHTML = "Gestão de Utilizadores";
	// else if (tab === "stats")
	// 	document.getElementById("tab-title").innerHTML = "Estatísticas";
	buildResultsList(tab, search, page);
	document.getElementById("tab-div").style.display = "block";
	document.getElementById("loading-modal-div").style.display = "none";
}

function buildResultsList(tab, search, page) {
	console.log(sessionStorage.getItem("myToken"));
	var options = {
		headers: {
			"Content-Type": "application/json",
			"Authorization": `Bearer ${sessionStorage.getItem("myToken")}`,
		},
	};
	fetch(apiUrl + `items/${tab}/${search}/${page}`, options).then((response) => {
		if (!response.ok) {
			throw new Error('Network response was not ok');
		}
		return response.json();
	}).then((data) => {
		if ((data.table).length == 0)
			return alert("Pesquisa não retornou resultados!");
		items = data;
		Object.assign(items, { "search" : search});
		createResultTable();
		buildPagination();
	}).catch((error) => {
		console.error("Error:", error);
	});
}

function createResultTable() {
	let resultsList = document.getElementById("results-list-div");
	if (resultsList.firstElementChild)
		resultsList.removeChild(resultsList.firstElementChild);
	let table = document.createElement("table");
	table.setAttribute("id", `result-table`);
	table.setAttribute("class", "table table--result");
	let i = 0;
	let head = document.createElement("thead");
	Object.entries(items.table[0]).forEach(([key, value]) => {
		let headMember = document.createElement("th");
		headMember.innerHTML = `${key}`;
		head.appendChild(headMember);
	});
	table.appendChild(head);
	let body = document.createElement("tbody");
	while (i < (items.table).length)
	{
		let row = document.createElement("tr");
		Object.entries(items.table[i]).forEach(([key, value]) => {
			let rowMember = document.createElement("td");
			rowMember.innerHTML = `${value}`;
			row.appendChild(rowMember);
		});
		body.appendChild(row);
		i++;
	}
	table.appendChild(body);
	resultsList.appendChild(table);
}

function buildPagination() {
	let pagination = document.getElementById("result-list-pagination-div");
	while (pagination.firstElementChild)
		pagination.removeChild(pagination.firstElementChild);
	let node = document.createElement("a");
	node.setAttribute("onclick", `changePage("<<");`);
	node.textContent = `<<`;
	pagination.appendChild(node);
	node = document.createElement("a");
	node.setAttribute("onclick", `changePage("<");`);
	node.textContent = `<`;
	pagination.appendChild(node);
	let startPage = Math.max(1, items.currPage - Math.floor(5 / 2));
	let endPage = Math.min(items.totalPages, startPage + 5);
	if (endPage - startPage <= 4) {
		startPage = Math.max(1, endPage - 5);
	}
	while (startPage <= endPage) {
		node = document.createElement("a");
		node.setAttribute("onclick", `changePage(${startPage});`);
		if (startPage == items.currPage) {
			node.setAttribute("class", "active");
		}
		node.textContent = startPage;
		pagination.appendChild(node);
		startPage++;
	}
	node = document.createElement("a");
	node.setAttribute("onclick", `changePage(">");`);
	node.textContent = `>`;
	pagination.appendChild(node);
	node = document.createElement("a");
	node.setAttribute("onclick", `changePage(">>");`);
	node.textContent = `>>`;
	pagination.appendChild(node);
}

function changePage(pageTo) {
	if (pageTo == "<<")
		changeTab(currentTab, items.search, 1);
	else if (pageTo == ">>")
		changeTab(currentTab, items.search, items.totalPages);
	else if (pageTo == "<") {
		if (items.currPage - 1 >= 1)
			changeTab(currentTab, items.search, items.currPage - 1);
		else
			return;
	}
	else if (pageTo == ">") {
		if (items.currPage + 1 < items.totalPages)
			changeTab(currentTab, items.search, items.currPage + 1);
		else
			return;
	}
	else
		changeTab(currentTab, items.search, pageTo);
}