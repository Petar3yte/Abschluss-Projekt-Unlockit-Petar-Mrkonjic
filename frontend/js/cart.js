// Ein globales Objekt, um von überall auf die Warenkorb-Funktionen zugreifen zu können
window.Cart = {
    /**
     * Fügt ein Produkt zum Warenkorb hinzu.
     * @param {string} productUuid - Die UUID des Produkts.
     * @param {number} quantity - Die hinzuzufügende Menge.
     */
    async addToCart(productUuid, quantity = 1) {
        const token = localStorage.getItem('authToken');

        if (token) {
            // Benutzer ist angemeldet -> API-Aufruf
            await this.apiCall('POST', 'items', { productUuid, quantity });
        } else {
            // Gast -> localStorage bearbeiten
            const cart = this.getLocalCart();
            const existingItem = cart.find(item => item.uuid === productUuid);
            if (existingItem) {
                existingItem.quantity += quantity;
            } else {
                cart.push({ uuid: productUuid, quantity });
            }
            this.saveLocalCart(cart);
            this.updateCounter();
        }
    },

    /**
     * Entfernt ein Produkt vollständig aus dem Warenkorb.
     * @param {string} productUuid - Die UUID des zu entfernenden Produkts.
     */
    async removeFromCart(productUuid) {
        const token = localStorage.getItem('authToken');

        if (token) {
            await this.apiCall('DELETE', `items/${productUuid}`);
        } else {
            let cart = this.getLocalCart();
            cart = cart.filter(item => item.uuid !== productUuid);
            this.saveLocalCart(cart);
            this.updateCounter();
        }
    },

    /**
     * Ändert die Menge eines Artikels im Warenkorb.
     * @param {string} productUuid - Die UUID des Produkts.
     * @param {number} newQuantity - Die neue Menge des Produkts.
     */
    async changeQuantity(productUuid, newQuantity) {
        const token = localStorage.getItem('authToken');
        const quantity = parseInt(newQuantity, 10);

        if (quantity <= 0) {
            await this.removeFromCart(productUuid);
            return;
        }

        if (token) {
            await this.apiCall('PUT', `items/${productUuid}`, { productUuid, quantity });
        } else {
            const cart = this.getLocalCart();
            const itemToUpdate = cart.find(item => item.uuid === productUuid);
            if (itemToUpdate) {
                itemToUpdate.quantity = quantity;
            }
            this.saveLocalCart(cart);
            this.updateCounter();
        }
    },

    // ---------------------------------------------------------------------------------
    // HELFERFUNKTIONEN
    // ---------------------------------------------------------------------------------

    /**
     * Zentralisierte Funktion für API-Aufrufe an den Cart-Controller.
     * @param {string} method - HTTP-Methode (POST, PUT, DELETE).
     * @param {string} endpoint - Der spezifische Endpunkt (z.B. 'items').
     * @param {object} [body=null] - Der Request-Body.
     */
    async apiCall(method, endpoint = '', body = null) {
        const token = localStorage.getItem('authToken');
        try {
            const options = {
                method: method,
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            };
            if (body) {
                options.body = JSON.stringify(body);
            }

            const response = await fetch(`https://localhost:7007/api/Cart/${endpoint}`, options);

            if (!response.ok) {
                throw new Error('API-Aufruf fehlgeschlagen');
            }

            const updatedServerCart = await response.json();
            // Server-Antwort als neue lokale Wahrheit speichern
            const localFormat = updatedServerCart.map(item => ({ uuid: item.productUuid, quantity: item.quantity }));
            this.saveLocalCart(localFormat);
            this.updateCounter();

        } catch (error) {
            console.error('Fehler bei der Warenkorb-Aktion:', error);
            alert('Ein Fehler ist aufgetreten. Bitte versuchen Sie es erneut.');
        }
    },

    getLocalCart: () => JSON.parse(localStorage.getItem('unlockitCart')) || [],
    saveLocalCart: (cart) => localStorage.setItem('unlockitCart', JSON.stringify(cart)),
    updateCounter: () => {
        const cartCounter = document.getElementById('cart-counter');
        if (cartCounter) {
            const cart = JSON.parse(localStorage.getItem('unlockitCart')) || [];
            const totalItems = cart.reduce((sum, item) => sum + item.quantity, 0);
            cartCounter.textContent = totalItems;
            cartCounter.style.display = totalItems > 0 ? 'inline-block' : 'none';
        }
        if (document.getElementById('cart-items-container')) {
            displayCart();
        }
    },
    clearLocalCart: () => {
        localStorage.removeItem('unlockitCart');
        Cart.updateCounter();
    }
};

async function displayCart() {
    const container = document.getElementById('cart-items-container');
    const summaryContainer = document.getElementById('cart-summary');
    if (!container || !summaryContainer) return;

    const cart = Cart.getLocalCart();
    container.innerHTML = '<h4>Lade Warenkorb...</h4>';

    if (cart.length === 0) {
        container.innerHTML = '<div class="card product-card"><div class="card-body text-center"><p class="lead m-0">Dein Warenkorb ist leer.</p></div></div>';
        summaryContainer.classList.add('d-none');
        return;
    }

    try {
        const productUuids = cart.map(item => item.uuid);       
        const response = await fetch(`https://localhost:7007/api/Products/details`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(productUuids)
        });

        if (!response.ok) throw new Error("Produktdetails konnten nicht geladen werden.");

        const productDetails = await response.json();
        let totalAmount = 0;
        container.innerHTML = ''; // Container leeren

        cart.forEach(item => {
            const product = productDetails.find(p => p.productUUID === item.uuid);
            if (product) {
                const itemTotal = product.price * item.quantity;
                totalAmount += itemTotal;
                
                const imageUrl = product.mainImageUrl ? `https://localhost:7007${product.mainImageUrl}` : 'https://placehold.co/150x150/1a1a2e/ffffff?text=No+Image';

                container.innerHTML += `
                    <div class="card product-card mb-3">
                        <div class="row g-0 align-items-center py-3">
                            <div class="col-md-2 text-center">
                                <img src="${imageUrl}" class="img-fluid rounded-start p-2" alt="${product.name}" style="max-height: 120px; width: auto;">
                            </div>
                            <div class="col-md-5">
                                <h5 class="card-title mb-1">${product.name}</h5>
                                <p class="card-text price m-0">${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(product.price)}</p>
                            </div>
                            <div class="col-md-2">
                                <div class="d-flex justify-content-center align-items-center">
                                    <button class="btn btn-secondary btn-sm" onclick="handleChangeQuantity('${item.uuid}', ${item.quantity - 1})">-</button>
                                    <span class="mx-3 fs-5">${item.quantity}</span>
                                    <button class="btn btn-secondary btn-sm" onclick="handleChangeQuantity('${item.uuid}', ${item.quantity + 1})">+</button>
                                </div>
                            </div>
                            <div class="col-md-3 text-end pe-3">
                                <p class="card-text fw-bold m-0 fs-5">${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(itemTotal)}</p>
                                <button class="btn btn-link text-danger btn-sm btn-remove mt-1" onclick="handleRemove('${item.uuid}')">Entfernen</button>
                            </div>
                        </div>
                    </div>
                `;
            }
        });
        
        const summaryDiv = document.getElementById('cart-summary');
        summaryDiv.classList.remove('d-none');
        summaryDiv.innerHTML = `
            <h3 id="cart-total">Gesamtsumme: ${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(totalAmount)}</h3>
            <a href="checkout.html" class="btn btn-primary-neon btn-lg mt-2">Zur Kasse</a>
        `;

    } catch (error) {
        console.error("Fehler beim Anzeigen des Warenkorbs:", error);
        container.innerHTML = '<h4 class="text-danger">Der Warenkorb konnte nicht geladen werden.</h4>';
    }
}

window.handleRemove = (uuid) => {
    if (confirm('Möchten Sie dieses Produkt wirklich aus dem Warenkorb entfernen?')) {
        Cart.removeFromCart(uuid).then(() => displayCart());
    }
};

window.handleChangeQuantity = (uuid, newQuantity) => {
    Cart.changeQuantity(uuid, newQuantity).then(() => displayCart());
};

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('cart-items-container')) {
        displayCart();
    }
});