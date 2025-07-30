document.addEventListener('DOMContentLoaded', () => {
    
    window.syncCartWithServer = async function() {
        const token = localStorage.getItem('authToken');
        
        if (!token) {
            updateCartCounter();
            return;
        }

        try {
            const response = await fetch('https://localhost:7007/api/Cart', {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (response.status === 401) {
                localStorage.removeItem('authToken');
                localStorage.removeItem('currentUser');
                localStorage.removeItem('unlockitCart'); // Auch hier bei Fehler leeren
                checkLoginStatus();
                return;
            }

            if (!response.ok) {
                throw new Error('Server-Warenkorb konnte nicht geladen werden.');
            }

            const serverCartItems = await response.json();
            
            const localCartFormat = serverCartItems.map(item => ({
                uuid: item.productUuid,
                quantity: item.quantity
            }));
            
            localStorage.setItem('unlockitCart', JSON.stringify(localCartFormat));
            
        } catch (error) {
            console.error('Fehler bei der Warenkorb-Synchronisierung:', error.message);
        } finally {
            updateCartCounter();
        }
    };

    function updateCartCounter() {
        const cartCounter = document.getElementById('cart-counter');
        if (cartCounter) {
            const cart = JSON.parse(localStorage.getItem('unlockitCart')) || [];
            const totalItems = cart.reduce((sum, item) => sum + item.quantity, 0);
            
            cartCounter.textContent = totalItems;
            cartCounter.style.display = totalItems > 0 ? 'inline-block' : 'none';
        }
    }

    function checkLoginStatus() {
        const userActionsContainer = document.getElementById('user-actions');
        const token = localStorage.getItem('authToken');
        const currentUserString = localStorage.getItem('currentUser');
    
        if (token && currentUserString && userActionsContainer) {
            try {
                const user = JSON.parse(currentUserString);
                const userName = user.userName || 'Benutzer';
                const isCheckoutPage = window.location.pathname.endsWith('checkout.html');
    
                //Profilbild
                let profilePicHtml = '';
                if (user.profilePictureUrl) {
                    // Stellt sicher, dass die URL korrekt ist, egal ob absolut oder relativ
                    const imageUrl = user.profilePictureUrl.startsWith('https://') 
                        ? user.profilePictureUrl 
                        : `https://localhost:7007${user.profilePictureUrl}`;
                    profilePicHtml = `<img src="${imageUrl}" alt="Profilbild" class="navbar-profile-pic">`;
                }
    
                let dashboardLinkHtml = '';
                if (user.role === 'Admin' || user.role === 'Mitarbeiter') {
                    dashboardLinkHtml = `
                        <li><hr class="dropdown-divider"></li>
                        <li><a class="dropdown-item" href="admin-login.html">Dashboard</a></li>
                    `;
                }
    
                let cartLinkHtml = '';
                if (!isCheckoutPage) {
                    cartLinkHtml = `
                        <a href="cart.html" class="nav-link me-3 cart-link">
                            <i class="bi bi-cart-fill"></i> WARENKORB
                            <span id="cart-counter" class="badge bg-danger ms-1">0</span>
                        </a>`;
                }
                
                userActionsContainer.innerHTML = `
                    ${cartLinkHtml}
                    ${profilePicHtml}
                    <div class="dropdown">
                        <a href="#" class="nav-link dropdown-toggle" data-bs-toggle="dropdown" aria-expanded="false">
                         Hallo, ${userName}
                        </a>
                        <ul class="dropdown-menu dropdown-menu-dark">
                            <li><a class="dropdown-item" href="account.html">Mein Konto</a></li>
                            <li><a class="dropdown-item" href="my-orders.html">Meine Bestellungen</a></li>
                            ${dashboardLinkHtml}
                            <li><hr class="dropdown-divider"></li>
                            <li><button id="logout-button" class="dropdown-item">Abmelden</button></li>
                        </ul>
                    </div>    
                `;
    
                document.getElementById('logout-button').addEventListener('click', () => {
                    localStorage.removeItem('authToken');
                    localStorage.removeItem('currentUser');
                    localStorage.removeItem('unlockitCart'); 
                    window.location.href = 'index.html';
                });
    
            } catch (error) {
                console.error("Fehler bei der Verarbeitung der Benutzerdaten:", error);
                 if (userActionsContainer) {
                     userActionsContainer.innerHTML = '';
                 }
            }
        } else if (userActionsContainer) {
            userActionsContainer.innerHTML = `
                <a href="cart.html" class="nav-link me-3 cart-link">
                    <i class="bi bi-cart-fill"></i> WARENKORB 
                    <span id="cart-counter" class="badge bg-danger ms-1">0</span>
                </a>
                <a href="shop-login.html" class="nav-link me-2">ANMELDEN</a>
                <a href="shop-register.html" class="btn btn-primary-neon">REGISTRIEREN</a>
            `;
        }

    const searchInput = document.getElementById('live-search-input');
    const searchResults = document.getElementById('live-search-results');
    const searchForm = document.getElementById('live-search-form');
    let searchTimeout;

    if(searchInput) {
        searchInput.addEventListener('input', () => {
            clearTimeout(searchTimeout);
            const searchTerm = searchInput.value;
            if (searchTerm.length > 2) {
                searchTimeout = setTimeout(() => {
                    performLiveSearch(searchTerm);
                }, 300);
            } else {
                searchResults.innerHTML = '';
            }
        });
    }
    
    document.addEventListener('click', (e) => {
        if (searchResults && !e.target.closest('.search-wrapper')) {
            searchResults.innerHTML = '';
        }
    });

    updateCartCounter();
}   
    
    checkLoginStatus(); 
    window.syncCartWithServer();

    if ('serviceWorker' in navigator) {
        window.addEventListener('load', () => {
            navigator.serviceWorker.register('/sw.js').then(registration => {
                console.log('ServiceWorker-Registrierung erfolgreich mit Scope: ', registration.scope);
            }, err => {
                console.log('ServiceWorker-Registrierung fehlgeschlagen: ', err);
            });
        });
    }

    async function performLiveSearch(searchTerm) {
        const searchResultsContainer = document.getElementById('live-search-results');
        if (!searchResultsContainer) return;
    
        // Erstelle die URL für die API-Anfrage.
        // encodeURIComponent sorgt dafür, dass Sonderzeichen wie '&' oder ' ' korrekt übertragen werden.
        const apiUrl = `https://localhost:7007/api/Products?searchTerm=${encodeURIComponent(searchTerm)}`;
    
        try {
            const response = await fetch(apiUrl);
            if (!response.ok) {
                throw new Error('Netzwerkantwort war nicht in Ordnung.');
            }
            const products = await response.json();
    
            // Leere vorherige Suchergebnisse.
            searchResultsContainer.innerHTML = '';
    
            if (products.length === 0) {
                // Zeige eine Nachricht an, wenn nichts gefunden wurde.
                searchResultsContainer.innerHTML = '<div class="live-result-item">Keine Produkte gefunden.</div>';
            } else {
                // Erstelle für jedes gefundene Produkt ein Ergebnis-Element.
                products.forEach(product => {
                    const itemLink = document.createElement('a');
                    itemLink.href = `shop-product-detail.html?uuid=${product.productUUID}`;
                    itemLink.className = 'live-result-item';
    
                    // Füge ein Platzhalterbild hinzu, wenn kein Hauptbild vorhanden ist.
                    const imageUrl = product.mainImageUrl 
                        ? `https://localhost:7007${product.mainImageUrl}` 
                        : 'https://placehold.co/50x50/1a1a2e/ffffff?text=N/A';
    
                    itemLink.innerHTML = `
                        <img src="${imageUrl}" width="50" height="50" class="me-3" alt="${product.name}">
                        <div class="d-flex flex-column">
                            <span>${product.name}</span>
                            <small class="price">${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(product.price)}</small>
                        </div>
                    `;
                    searchResultsContainer.appendChild(itemLink);
                });
            }
        } catch (error) {
            console.error('Fehler bei der Live-Suche:', error);
            searchResultsContainer.innerHTML = '<div class="live-result-item text-danger">Fehler bei der Suche.</div>';
        }
    }
});