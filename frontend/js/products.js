document.addEventListener('DOMContentLoaded', () => {
    const token = localStorage.getItem('authToken');

    const currentUserString = localStorage.getItem('currentUser');
    let userRole = null;

    if (!token || !currentUserString) {
        window.location.href = 'login.html';
        return;
    }

    try {
        const currentUser = JSON.parse(currentUserString);
        userRole = currentUser.role;
    } catch (e) {
        console.error("Benutzerdaten konnten nicht verarbeitet werden.", e);
        window.location.href = 'admin-login.html';
        return;
    }


    const logoutButton = document.getElementById('logout-button');
    if (logoutButton) {
        logoutButton.addEventListener('click', () => {
            localStorage.removeItem('authToken');
            localStorage.removeItem('currentUser');
            window.location.href = 'admin-login.html';
        });
    }

    const productTableBody = document.getElementById('product-table-body');

    async function fetchProducts() {
        try {
            const response = await fetch('https://localhost:7007/api/Products/admin/all', {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const products = await response.json();
            productTableBody.innerHTML = ''; 

            if (products.length === 0) {
                 productTableBody.innerHTML = '<tr><td colspan="8" class="text-center">Noch keine Produkte angelegt.</td></tr>';
            } else {
                products.forEach(product => {
                    const row = document.createElement('tr');

                    if (!product.isActive) {
                        row.classList.add('table-dark', 'text-muted');
                        row.style.textDecoration = 'line-through';
                    } else if (product.stockQuantity === 0) {
                        row.classList.add('table-danger'); 
                    } else if (product.stockQuantity <= 10) {
                        row.classList.add('table-warning'); 
                    }
                    
                    const statusBadge = product.isActive 
                        ? `<span class="badge bg-success">Aktiv</span>` 
                        : `<span class="badge bg-secondary">Inaktiv</span>`;

                    let actionsHtml = `<a href="product-edit.html?uuid=${product.productUUID}" class="btn btn-sm btn-info">Details</a>`;

                    if (userRole === 'Admin') {
                        const toggleButtonText = product.isActive ? 'Deaktivieren' : 'Aktivieren';
                        const toggleButtonClass = product.isActive ? 'btn-danger' : 'btn-success';
                        actionsHtml += `
                            <button class="btn btn-sm ${toggleButtonClass} btn-toggle-status ms-1" 
                                    data-uuid="${product.productUUID}" 
                                    data-active="${product.isActive}">
                                ${toggleButtonText}
                            </button>
                        `;
                    }

                    row.innerHTML = `
                        <td>${product.name}</td>
                        <td>${product.categoryName || 'N/A'}</td>
                        <td>${product.platforms.join(', ') || 'N/A'}</td>
                        <td>${product.genres.join(', ') || 'N/A'}</td>
                        <td>${product.price.toFixed(2)} €</td>
                        <td>${product.stockQuantity}</td>
                        <td>${statusBadge}</td>
                        <td>
                            <div class="btn-group" role="group">
                                ${actionsHtml}
                            </div>
                        </td>
                    `;
                    productTableBody.appendChild(row);
                });
            }

        } catch (error) {
            console.error('Fehler beim Abrufen der Produkte:', error);
            productTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Fehler beim Laden der Produktdaten.</td></tr>';
        }
    }

    async function toggleProductStatus(uuid, isActive) {
        const action = isActive ? 'deaktivieren' : 'aktivieren';
        const url = isActive 
            ? `https://localhost:7007/api/Products/${uuid}` 
            : `https://localhost:7007/api/Products/${uuid}/reactivate`; 

        const method = isActive ? 'DELETE' : 'POST';

        if (!confirm(`Möchtest du dieses Produkt wirklich ${action}?`)) {
            return;
        }

        try {
            const response = await fetch(url, {
                method: method,
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (response.ok) {
                await fetchProducts();
            } else {
                throw new Error(`Produkt konnte nicht ${action} werden.`);
            }
        } catch (error) {
            console.error('Fehler beim Ändern des Produktstatus:', error);
            alert(error.message);
        }
    }

    productTableBody.addEventListener('click', (event) => {
        if (event.target.classList.contains('btn-toggle-status')) {
            const productUuid = event.target.dataset.uuid;
            const isActive = event.target.dataset.active === 'true'; 
            toggleProductStatus(productUuid, isActive);
        }
    });

    fetchProducts();
});