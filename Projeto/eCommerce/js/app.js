let carrinho = [];
let usuarios = [];
let usuarioLogado = null;
let mostrandoFavoritos = false;
let mostrandoMaisComprados = false;

// Geração dos cursos na página
function generateCursos() {
    const p = document.getElementById("lista-cursos");
    p.innerHTML = createCourseHTML(cursos);
    adicionarEventList();
}

// Função para adicionar ao carrinho
function adicionarAoCarrinho(evt) {
    evt.preventDefault();
    // vai buscar o id, nome e preço do card no qual o botão foi clicado
    const id = evt.target.getAttribute('data-id');
    const nome = evt.target.getAttribute('data-nome');
    const preco = evt.target.getAttribute('data-preco');

    const itemExistente = carrinho.find(item => item.id === id); // Verifica se o curso já está no carrinho e se já estiver aumenta a quantidade
    if (itemExistente) {
        itemExistente.quantidade++;
    } else {
        carrinho.push({ id, nome, preco, quantidade: 1 }); // agr o array de carrinho vai ter um objeto com estes valores
    }

    atualizarCarrinho();
}

// Atualizar carrinho
function atualizarCarrinho() {
    const listaCarrinho = document.getElementById("lista-carrinho").getElementsByTagName('tbody')[0]; // Obtém a tabela do carrinho
    listaCarrinho.innerHTML = "";

    carrinho.forEach(item => {
        const curso = cursos.find(curso => curso.ID === item.id); // aqui vai encontrar o curso dependendo do id
        const imagePath = curso ? curso.Imagem[0] : ''; // Obtém o caminho da imagem, se o curso existir


        const row = listaCarrinho.insertRow(); // construção da tabela
        row.innerHTML = `
            <td><img src="${imagePath}" alt="${item.nome}" class="imagen-curso" style="width: 50px;"></td>
            <td>${item.nome}</td>
            <td>${item.preco}</td>
            <td>${item.quantidade}</td>
            <td><button class="remover-item" data-id="${item.id}">Remover</button></td>
        `;
    });

    const removerButtons = document.querySelectorAll('.remover-item');
    removerButtons.forEach(button => {
        button.addEventListener('click', removerDoCarrinho); // adição do evento de click no botão de remover
    });
}

document.getElementById('formLogin').addEventListener("submit", function(evt) {
    evt.preventDefault(); // Impede o comportamento padrão de envio do formulário
    logarUsuario();
});

// Remover do carrinho
function removerDoCarrinho(evt) {
    const id = evt.target.getAttribute('data-id');
    const item = carrinho.find(item => item.id === id); // encontra o id no carrinho

    if (item) {
        item.quantidade--;
    }
    if (item.quantidade <= 0) {
        carrinho = carrinho.filter(item => item.id !== id); // tira o item se for menor ou igual a zoro, também diminui a quantidade se for o contrário
    } //verifica se o id do item atual no carrinho é diferente do id que queremos remover. Se for diferente, esse item será mantido no novo array.

    atualizarCarrinho();
}

// Limpar carrinho
function cleanCarrinho() {
    carrinho = [];
    atualizarCarrinho();
}

// Barra de pesquisa
const searchInput = document.getElementById('buscador');
const submitButton = document.getElementById('submit-buscador');

submitButton.addEventListener('click', (evt) => { 
    evt.preventDefault(); 
});

searchInput.addEventListener('input', () => {    // Adiciona um evento de entrada ao campo de busca
    const query = searchInput.value.toLowerCase();  // Obtém o valor da busca em minúsculas
    const resultsContainer = document.getElementById('lista-cursos');
    resultsContainer.innerHTML = ''; // limpa o html por enquanto

    const filteredCursos = cursos.filter(curso =>
        curso.Nome[0].toLowerCase().includes(query) ||
        curso.Autor[0].toLowerCase().includes(query) || //Verifica se tem algum destes valores na pesquisa
        curso.Preco[0].includes(query)
    );

    if (filteredCursos.length > 0) {
        resultsContainer.innerHTML = createCourseHTML(filteredCursos);  // Cria o html filtrado
    } else {
        resultsContainer.innerHTML = '<p class="vazio">Nenhum curso encontrado.</p>';
    }

    adicionarEventList(); // outra vez o evento no adicionar ao carrinho
});

// Função para favoritar
function favorite(event) {
    const id = event.target.getAttribute('data-id');  //data-id do elemento que disparou o evento
    const curso = cursos.find(c => c.ID === id); // o c representa um curso da lista de cursos

    if (curso) {
        curso.fav = !curso.fav; //Se curso.fav for true, ele se tornará false e se for false, ele se tornará true
        event.target.classList.toggle('favorito', curso.fav); //Aqui adiciona e retira a class css 
    }
    salvarFavoritosNoLocalStorage();
}

function salvarFavoritosNoLocalStorage() {
    const favoritos = cursos.filter(curso => curso.fav).map(curso => curso.ID); // map para transforma esse array de cursos em um array que contém apenas os IDs desses cursos.
    localStorage.setItem('cursosFavoritos', JSON.stringify(favoritos)); // Guarda o array de ids favoritos no localStorage
}

function carregarFavoritosDoLocalStorage() {
    const favoritos = JSON.parse(localStorage.getItem('cursosFavoritos')) || [];
    //  Se essa chave existir, o método retorna a string JSON que foi armazenada anteriormente. Caso contrário, retorna null.
    cursos.forEach(curso => {
        if (favoritos.includes(curso.ID)) {
            curso.fav = true; // aqui muda o atributo dos id´s para true, isto meti para funcionar no onload
        }
    });
}

document.getElementById('toggle-favorites').addEventListener('click', () => {
    mostrandoFavoritos = !mostrandoFavoritos;

    if (mostrandoFavoritos) {
        mostrarCursosFavoritos();
    } else {
        generateCursos();
    }
});

// Função para mostrar cursos favoritos
function mostrarCursosFavoritos() {
    const cursosFavoritos = cursos.filter(curso => curso.fav);
    const p = document.getElementById("lista-cursos");
    p.innerHTML = '';

    if (cursosFavoritos.length === 0) {
        p.innerHTML = '<p class="vazio">Não tem nenhum item nos favoritos, adicione primeiro para ver a sua lista!</p>';
        return;
    }
    
    p.innerHTML = createCourseHTML(cursosFavoritos);
    adicionarEventList();
}

function mostrarItensMaisComprados() {
    const itensMaisComprados = cursos.sort((a, b) => b.Rating - a.Rating).slice(0, 5); // Mostra os 5 itens mais comprados

    const p = document.getElementById("lista-cursos");
    p.innerHTML = '';

    if (itensMaisComprados.length === 0) {
        p.innerHTML = '<p class="vazio">Nenhum item comprado ainda.</p>';
        return;
    }

    p.innerHTML = createCourseHTML(itensMaisComprados);
    adicionarEventList();
}

document.getElementById('toggle-mais-comprados').addEventListener('click', () => {
    mostrandoMaisComprados = !mostrandoMaisComprados;

    if (mostrandoMaisComprados) {
        mostrarItensMaisComprados(); // Chama a função para mostrar itens mais comprados ao abrir
    } else {
        generateCursos(); // Retorna à lista de cursos normal
    }
});

let categoriasVisiveis = false; // Variável para controlar a visibilidade das categorias

function mostrarCategorias() {
    const categoriasUnicas = [...new Set(cursos.map(curso => curso.Categoria[0]))];
    const listaCategorias = document.getElementById('lista-categorias');
    listaCategorias.innerHTML = '';

    // Para cada categoria única encontrada
    categoriasUnicas.forEach(categoria => {
        const li = document.createElement('li'); // Cria um novo item de lista
        li.textContent = categoria; // Define o texto do item como o nome da categoria
        li.dataset.categoria = categoria; // Adiciona o atributo de dados com o nome da categoria
        li.classList.add('button'); // adiciona também a class de button do css

        li.addEventListener('click', function() {
            mostrarCursosPorCategoria(categoria); // adiciona o evento de clique
        });
        listaCategorias.appendChild(li); // os itens da lista vai para o id das categorias
    });

    const categoriasDiv = document.getElementById('categorias');
    categoriasVisiveis = !categoriasVisiveis;
    categoriasDiv.style.display = categoriasVisiveis ? 'block' : 'none'; // toggle do botão das categorias
}

function mostrarCursosPorCategoria(categoria) {
    const listaCursos = document.getElementById('lista-cursos');
    listaCursos.innerHTML = '';

    const cursosFiltrados = cursos.filter(curso => curso.Categoria[0] === categoria);// aqui o curso é filtrado pela a variável de categoria que o mesmo tem
    listaCursos.innerHTML = createCourseHTML(cursosFiltrados); // outra vez a criação do html dos cursos e do evento de clique 
    adicionarEventList()
}

document.getElementById('toggle-categorias').addEventListener('click', mostrarCategorias);

// Registro de utilizador
function registrarUsuario() {
    const nome = document.getElementById('nome').value;
    const email = document.getElementById('email').value;
    const senha = document.getElementById('senha').value;

    const usuarioExistente = usuarios.find(u => u.email === email); // verifica no base de dados essa combinação é já existente
    if (usuarioExistente) {
        alert('Este email já está registrado!');
        return;
    }

    usuarios.push({ nome, email, senha }); // puxa para o array de usuarios
    localStorage.setItem('usuarios', JSON.stringify(usuarios));
    alert('Usuário registrado com sucesso!');
    toggleModal('registroModal', false); // fecha o modal, a caixinha de registro
}

function logarUsuario() {
    const email = document.getElementById('loginEmail').value;
    const senha = document.getElementById('loginSenha').value;

    const usuario = usuarios.find(u => u.email === email && u.senha === senha); // verifica se tem a combinação de email e senha
    if (usuario) {
        usuarioLogado = usuario; //usuário fica logado na sua conta guardada no array
        alert(`Bem-vindo, ${usuario.nome}!`);
        toggleModal('loginModal', false);
        atualizarBotoes();
    } else {
        alert('Email ou senha incorretos!');
    }
}

function resetUsuario() {
    usuarioLogado = null;
    atualizarBotoes();
}

function atualizarBotoes() {
    const caixasDeRegistro = document.getElementById('caixasDeRegistro');
    caixasDeRegistro.innerHTML = '';

    if (usuarioLogado) {
        caixasDeRegistro.innerHTML = '<button class="button" onclick="resetUsuario()">Encerrar sessão</button>'; //se usuario está logado, terá um botao de encerrar sessão
    } else {
        const btnRegistrar = document.createElement('button');
        btnRegistrar.textContent = 'Registrar';
        btnRegistrar.onclick = () => toggleModal('registroModal', true);
        
        const btnLogin = document.createElement('button');
        btnLogin.textContent = 'Login';
        btnLogin.onclick = () => toggleModal('loginModal', true); //se não os dois botões tomam o lugar, no onclick faz aparece o modal de login ou registro

        caixasDeRegistro.appendChild(btnRegistrar);
        caixasDeRegistro.appendChild(btnLogin);
    }
}

// Função para alternar a visibilidade dos modais
function toggleModal(modalId) {
    const modal = document.getElementById(modalId);
    modal.style.display = modal.style.display === 'block' ? 'none' : 'block';
}

// Eventos para o formulário de registro e login
document.getElementById('formRegistro').addEventListener("submit", function(evt) {
    evt.preventDefault();
    registrarUsuario();
});

function compra() {
    if (usuarioLogado) { // apenas deixará realizar a compra se tiver um usuário logado
        // Incrementa o número de ratings para cada item no carrinho
        carrinho.forEach(item => {
            const curso = cursos.find(c => c.ID === item.id); // outra vez faço o js buscar o item pelo id
            if (curso) {
                curso.Rating += item.quantidade; // Aumenta o rating pela quantidade comprada
            }
        });
        salvarRatings()
        alert('Compra finalizada');
        cleanCarrinho(); 
    } else {
        alert('Por favor faça login para finalizar esta compra!');
    }
}

function salvarRatings(){
const ratings = cursos.map(curso => ({id:curso.ID, rating: curso.Rating})); //guardo a quantidade do rating atual
localStorage.setItem('cursosRatings', JSON.stringify(ratings));
}

function carregarRatings () {
    const ratingSalvos = JSON.parse(localStorage.getItem('cursosRatings')) || []; //

    ratingSalvos.forEach (({id, rating}) => {
        const curso = cursos.find(c => c.ID === id);
        if (curso) {
            curso.Rating = rating ; // a propriedade Rating é definida para o valor de rating
        }
    });
}

// Carregar dados iniciais
window.onload = () => {
    const usuariosSalvos = JSON.parse(localStorage.getItem('usuarios'));
    if (usuariosSalvos) {
        usuarios = usuariosSalvos; // Carrega os usuários do localStorage
    }

    carregarRatings();
    generateCursos();
    carregarFavoritosDoLocalStorage();
    atualizarBotoes();
    resetUsuario()
};
