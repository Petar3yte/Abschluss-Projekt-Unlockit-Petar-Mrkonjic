//index.js
document.addEventListener('DOMContentLoaded', function () {
    const productListContainer = document.getElementById('product-list');    

    productListContainer.addEventListener('click', function(event) {
        if (event.target.classList.contains('add-to-cart-btn')) {
            const productUuid = event.target.dataset.productUuid;
            
            window.Cart.addToCart(productUuid, 1);
            
            // Kurzes visuelles Feedback für den Benutzer
            const button = event.target;
            button.textContent = 'Hinzugefügt!';
            button.classList.add('btn-success');
            setTimeout(() => {
                button.textContent = 'Zum Warenkorb';
                button.classList.remove('btn-success');
            }, 1500);
        }
    });

    async function fetchAndDisplayProducts() {
        const apiUrl = 'https://localhost:7007/api/products';
        try {
            const response = await fetch(apiUrl);
            if (!response.ok) throw new Error(`HTTP-Fehler!`);
            const products = await response.json();
            productListContainer.innerHTML = '';
            products.forEach(product => {
                const productCard = createProductCard(product);
                productListContainer.appendChild(productCard);
            });
        } catch (error) {
            console.error('Fehler beim Abrufen der Produkte:', error);
            productListContainer.innerHTML = '<p class="text-danger">Fehler beim Laden der Produkte.</p>';
        }
    }

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

    fetchAndDisplayProducts();
});