document.addEventListener('DOMContentLoaded', function () {
    const productDetailContainer = document.getElementById('product-detail-container');
    const urlParams = new URLSearchParams(window.location.search);
    const productUuid = urlParams.get('uuid');
    
    let currentImages = []; 
    let currentIndex = 0;   

    if (!productUuid) {
        productDetailContainer.innerHTML = '<p class="text-danger">Kein Produkt ausgewählt. Bitte gehen Sie zurück zur Produktübersicht.</p>';
        return;
    }

    async function fetchProductDetails() {
        try {
            const response = await fetch(`https://localhost:7007/api/Products/${productUuid}`);
            if (!response.ok) {
                throw new Error('Produkt nicht gefunden.');
            }
            const product = await response.json();
            renderProductDetails(product);
        } catch (error) {
            console.error('Fehler beim Abrufen der Produktdetails:', error);
            productDetailContainer.innerHTML = `<p class="text-danger">Fehler: ${error.message}</p>`;
        }
    }
    
    function displayCurrentImage() {
        const mainImageElement = document.getElementById('main-product-image');
        if (mainImageElement && currentImages.length > 0) {
            mainImageElement.src = `https://localhost:7007${currentImages[currentIndex].imageUrl}`;
        }
    }

    function renderProductDetails(product) {
        document.title = `${product.name} - Unlockit`;

        currentImages = product.images;
        currentIndex = currentImages.findIndex(img => img.isMainImage);
        if (currentIndex === -1) { 
            currentIndex = 0;
        }

        const imageGalleryHtml = `
            <div class="col-md-6 position-relative">
                <img id="main-product-image" class="img-fluid rounded d-block mx-auto" alt="${product.name}">
                
                ${currentImages.length > 1 ? `
                <button id="prev-image-btn" class="btn btn-image-nav prev">
                    <i class="bi bi-arrow-left-circle-fill"></i>
                </button>
                <button id="next-image-btn" class="btn btn-image-nav next">
                    <i class="bi bi-arrow-right-circle-fill"></i>
                </button>
                ` : ''}
            </div>
        `;
        let platformsHtml = '';
        if (product.platforms && product.platforms.length > 0) {
            platformsHtml = `
                <div class="mt-3">
                    <strong>Plattformen:</strong>
                    <div class="d-flex flex-wrap gap-2 mt-1">
                        ${product.platforms.map(platform => `<span class="badge bg-secondary">${platform}</span>`).join('')}
                    </div>
                </div>`;
        }

        let genresHtml = '';
        if (product.genres && product.genres.length > 0) {
            genresHtml = `
                <div class="mt-3">
                    <strong>Genres:</strong>
                    <div class="d-flex flex-wrap gap-2 mt-1">
                        ${product.genres.map(genre => `<span class="badge bg-secondary">${genre}</span>`).join('')}
                    </div>
                </div>`;
        }


        let buttonHtml;
        if (product.stockQuantity > 0) {
            buttonHtml = `<button id="add-to-cart-detail" class="btn btn-primary-neon btn-lg">Zum Warenkorb</button>`;
        } else {
            buttonHtml = `<button id="add-to-cart-detail" class="btn btn-secondary btn-lg" disabled>Nicht auf Lager</button>`;
        }
        
        const productHtml = `
            ${imageGalleryHtml}
            <div class="col-md-6">
                <h2>${product.name}</h2>
                <p class="price fs-4">${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(product.price)}</p>
                <p class="text-muted">Kategorie: ${product.categoryName || 'N/A'}</p>
                
                ${platformsHtml}
                ${genresHtml}

                <h4 class="mt-4">Beschreibung</h4>
                <p>${product.description || 'Keine Beschreibung verfügbar.'}</p>
                <div class="d-grid gap-2 mt-4">
                    ${buttonHtml}
                </div>
            </div>
        `;
        
        productDetailContainer.innerHTML = productHtml;

        displayCurrentImage(); 
        if (currentImages.length > 1) {
            document.getElementById('next-image-btn').addEventListener('click', () => {
                currentIndex++;
                if (currentIndex >= currentImages.length) {
                    currentIndex = 0;
                }
                displayCurrentImage();
            });

            document.getElementById('prev-image-btn').addEventListener('click', () => {
                currentIndex--;
                if (currentIndex < 0) {
                    currentIndex = currentImages.length - 1;
                }
                displayCurrentImage();
            });
        }

        if (product.stockQuantity > 0) {
            document.getElementById('add-to-cart-detail').addEventListener('click', () => {
                // Ruft die neue, globale Funktion aus cart.js auf
                window.Cart.addToCart(product.productUUID, 1);
            });
        }
    }    

    fetchProductDetails();
});