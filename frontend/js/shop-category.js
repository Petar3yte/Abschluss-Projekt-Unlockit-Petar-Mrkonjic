document.addEventListener('DOMContentLoaded', function () {
    const productListContainer = document.getElementById('product-list');
    const categoryTitleElement = document.getElementById('category-title');
    const platformFilter = document.getElementById('platform-filter');
    const genreFilter = document.getElementById('genre-filter');
    const applyFiltersBtn = document.getElementById('apply-filters-btn');
    
    const platformFilterContainer = document.getElementById('platform-filter-container');
    const genreFilterContainer = document.getElementById('genre-filter-container');
    const filterCard = document.getElementById('filter-card');

    function highlightActiveCategory() {
        const urlParams = new URLSearchParams(window.location.search);
        const currentCategory = urlParams.get('name');

        if (!currentCategory) return; 

        const navLinks = document.querySelectorAll('#shop-navbar .nav-link');

        navLinks.forEach(link => {
            if (link.textContent.trim().toLowerCase() === currentCategory.toLowerCase()) {
                link.classList.add('active'); 
            }
        });
    }

    async function loadFilters() {
        try {
            const [platformResponse, genreResponse] = await Promise.all([
                fetch('https://localhost:7007/api/Products/platforms'),
                fetch('https://localhost:7007/api/Products/genres')
            ]);

            if (!platformResponse.ok || !genreResponse.ok) {
                throw new Error('Filteroptionen konnten nicht geladen werden.');
            }

            const platforms = await platformResponse.json();
            const genres = await genreResponse.json();

            platforms.forEach(platform => {
                const option = new Option(platform.name, platform.name);
                platformFilter.add(option);
            });

            genres.forEach(genre => {
                const option = new Option(genre.name, genre.name);
                genreFilter.add(option);
            });

        } catch (error) {
            console.error(error);
        }
    }
    
    productListContainer.addEventListener('click', function(event) {
        if (event.target.classList.contains('add-to-cart-btn')) {
            const productUuid = event.target.dataset.productUuid;
            window.Cart.addToCart(productUuid);
        }
    });

    function createProductCard(product) {
        const colDiv = document.createElement('div');
        colDiv.className = 'col';
        const imageUrl = product.mainImageUrl ? `https://localhost:7007${product.mainImageUrl}` : 'https://placehold.co/300x200/1a1a2e/ffffff?text=No+Image';
    
        let buttonHtml;
        if (product.stockQuantity > 0) {
            buttonHtml = `<button class="btn btn-primary-neon mt-2 add-to-cart-btn" data-product-uuid="${product.productUUID}">Zum Warenkorb</button>`;
        } else {
            buttonHtml = `<button class="btn btn-secondary disabled mt-2" disabled>Nicht auf Lager</button>`;
        }
    
        const detailUrl = `shop-product-detail.html?uuid=${product.productUUID}`;
    
        const cardHtml = `
            <div class="card product-card h-100">
                <a href="${detailUrl}">
                    <img src="${imageUrl}" class="card-img-top" alt="${product.name}">
                </a>
                <div class="card-body d-flex flex-column">
                    <h5 class="card-title">
                        <a href="${detailUrl}" class="text-light text-decoration-none">${product.name}</a>
                    </h5>
                    <p class="card-text price mt-auto">${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(product.price)}</p>
                    ${buttonHtml}
                </div>
            </div>
        `;
        colDiv.innerHTML = cardHtml;
        return colDiv;
    }

    async function fetchAndDisplayProductsByCategory(categoryName, platformName = '', genreName = '') {
        document.title = `${categoryName} - Unlockit`;
        categoryTitleElement.textContent = categoryName;

        const params = new URLSearchParams({
            categoryName: categoryName
        });
        if (platformName) {
            params.append('platformName', platformName);
        }
        if (genreName) {
            params.append('genreName', genreName);
        }

        const apiUrl = `https://localhost:7007/api/Products?${params.toString()}`;

        try {
            productListContainer.innerHTML = '<p class="text-light">Lade Produkte...</p>';
            const response = await fetch(apiUrl);
            if (!response.ok) throw new Error(`HTTP-Fehler!`);
            
            const products = await response.json();
            productListContainer.innerHTML = '';
            
            if (products.length === 0) {
                productListContainer.innerHTML = '<p class="text-light">Für diese Filterkriterien wurden keine Produkte gefunden.</p>';
                return;
            }

            products.forEach(product => {
                const productCard = createProductCard(product);
                productListContainer.appendChild(productCard);
            });
        } catch (error) {
            console.error(`Fehler beim Abrufen der Produkte für Kategorie ${categoryName}:`, error);
            productListContainer.innerHTML = '<p class="text-danger">Fehler beim Laden der Produkte.</p>';
        }
    }

    const urlParams = new URLSearchParams(window.location.search);
    const categoryName = urlParams.get('name');
    
    highlightActiveCategory();

    if (categoryName) {
        //LOGIK ZUM ANZEIGEN/AUSBLENDEN DER FILTER
        if (categoryName.toLowerCase() === 'hardware') {
            if (genreFilterContainer) genreFilterContainer.style.display = 'none';
        } else if (categoryName.toLowerCase() === 'merch') {
            if (filterCard) filterCard.style.display = 'none';
        }

        loadFilters().then(() => {
            fetchAndDisplayProductsByCategory(categoryName); 
        });

        applyFiltersBtn.addEventListener('click', () => {
            const selectedPlatform = platformFilter.value;
            const selectedGenre = genreFilter.value;
            fetchAndDisplayProductsByCategory(categoryName, selectedPlatform, selectedGenre);
        });
    } else {
        categoryTitleElement.textContent = "Keine Kategorie ausgewählt";
        productListContainer.innerHTML = '<p class="text-warning">Bitte wählen Sie eine Kategorie aus.</p>';
    }
});