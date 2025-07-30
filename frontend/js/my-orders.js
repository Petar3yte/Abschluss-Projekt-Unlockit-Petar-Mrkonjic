document.addEventListener('DOMContentLoaded', function () {
    const orderContainer = document.getElementById('order-history-container');
    const limitSelect = document.getElementById('order-limit-select');
    const token = localStorage.getItem('authToken');

    if (!token) {
        window.location.href = 'shop-login.html';
        return;
    }

    const getStatusBadge = (status) => {
        switch (status) {
            case 'Ausstehend': return '<span class="badge bg-warning text-dark">Ausstehend</span>';
            case 'in_Bearbeitung': return '<span class="badge bg-info text-dark">In Bearbeitung</span>';
            case 'Versendet': return '<span class="badge bg-success">Versendet</span>';
            case 'Storniert': return '<span class="badge bg-danger">Storniert</span>';
            default: return `<span class="badge bg-secondary">${status}</span>`;
        }
    };

    function loadMyOrders() {
        const limit = limitSelect ? limitSelect.value : '10';
        let url = `https://localhost:7007/api/Orders/my`;

        if (limit) {
            url += `?limit=${limit}`;
        }

        orderContainer.innerHTML = '<p>Bestellungen werden geladen...</p>';

        fetch(url, {
            method: 'GET',
            headers: { 'Authorization': `Bearer ${token}` }
        })
        .then(response => {
            if (!response.ok) {
                if (response.status === 401 || response.status === 403) {
                    localStorage.clear();
                    window.location.href = 'shop-login.html';
                }
                throw new Error('Bestellungen konnten nicht geladen werden.');
            }
            return response.json();
        })
        .then(orders => {
            orderContainer.innerHTML = ''; 
            if (orders.length === 0) {
                orderContainer.innerHTML = '<p>Du hast noch keine Bestellungen getätigt.</p>';
                return;
            }

            const accordionContainer = document.createElement('div');
            accordionContainer.className = 'accordion';
            accordionContainer.id = 'ordersAccordion';

            orders.forEach((order, index) => {
                const orderDate = new Date(order.orderDate).toLocaleDateString('de-DE');
                const totalAmount = new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(order.totalAmount);
                const itemsHtml = order.items.map(item => `
                    <li class="list-group-item d-flex justify-content-between align-items-center">
                        <span>${item.quantity}x ${item.product.name}</span>
                        <span>${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(item.unitPrice)}</span>
                    </li>
                `).join('');

                let shippingAddressHtml = '<p class="text-muted">Lieferadresse nicht verfügbar.</p>';
                if (order.shippingAddressJson) {
                    try {
                        const address = JSON.parse(order.shippingAddressJson);
                        shippingAddressHtml = `
                            <div class="mb-3">
                                <strong>Lieferung an:</strong>
                                <p class="mb-0 mt-1">${address.Name}</p>
                                <p class="mb-0">${address.Line1}</p>
                                <p class="mb-0">${address.PostalCode} ${address.City}</p>
                                <p class="mb-0">${address.Country}</p>
                            </div>
                        `;
                    } catch (e) {
                        console.error('Fehler beim Parsen der Lieferadresse:', e);
                    }
                }

                let actionButtonsHtml = `<button class="btn btn-primary btn-sm btn-reorder" data-order-uuid="${order.orderUUID}">Erneut bestellen</button>`;
                if (order.orderStatus === 'Ausstehend' || order.orderStatus === 'in_Bearbeitung') {
                    actionButtonsHtml += `<button class="btn btn-outline-danger btn-sm ms-2 btn-cancel-order" data-order-uuid="${order.orderUUID}">Stornieren</button>`;
                }

                const orderElement = document.createElement('div');
                orderElement.className = 'accordion-item product-card';
                orderElement.innerHTML = `
                    <h2 class="accordion-header" id="heading-${index}">
                        <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapse-${index}">
                            <div class="w-100 d-flex justify-content-between align-items-center pe-3">
                                <span>Bestellung vom ${orderDate}</span>
                                ${getStatusBadge(order.orderStatus)}
                                <strong class="text-neon-pink">${totalAmount}</strong>
                            </div>
                        </button>
                    </h2>
                    <div id="collapse-${index}" class="accordion-collapse collapse" data-bs-parent="#ordersAccordion">
                        <div class="accordion-body">
                            ${shippingAddressHtml}
                            <strong>Bestellte Artikel:</strong>
                            <ul class="list-group list-group-flush mt-2 mb-3">${itemsHtml}</ul>
                            <div class="d-flex justify-content-end">
                                 ${actionButtonsHtml}
                            </div>
                        </div>
                    </div>
                `;
                accordionContainer.appendChild(orderElement);
            });
            orderContainer.appendChild(accordionContainer);
        })
        .catch(error => {
            console.error('Fehler:', error);
            orderContainer.innerHTML = `<p class="text-danger">${error.message}</p>`;
        });
    }

    orderContainer.addEventListener('click', (event) => {
        const target = event.target;
        if (target.classList.contains('btn-reorder')) {
            const orderUuid = target.dataset.orderUuid;
            if (orderUuid) handleReorder(orderUuid);
        }
        if (target.classList.contains('btn-cancel-order')) {
            const orderUuid = target.dataset.orderUuid;
            if (orderUuid) handleCancelOrder(orderUuid);
        }
    });

    async function handleCancelOrder(orderUuid) {
        if (!confirm('Möchtest du diese Bestellung wirklich stornieren? Dieser Vorgang kann nicht rückgängig gemacht werden.')) {
            return;
        }
        try {
            const response = await fetch(`https://localhost:7007/api/Orders/${orderUuid}/cancel`, {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Bestellung konnte nicht storniert werden.');
            }
            alert('Deine Bestellung wurde erfolgreich storniert.');
            loadMyOrders();
        } catch (error) {
            console.error('Fehler beim Stornieren:', error);
            alert(`Fehler: ${error.message}`);
        }
    }

    async function handleReorder(orderUuid) {
        if (!confirm('Möchtest du die Artikel aus dieser Bestellung in deinen Warenkorb legen?')) {
            return;
        }
        try {
            const response = await fetch(`https://localhost:7007/api/Orders/${orderUuid}/reorder`, {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}` }
            });
    
            if (response.ok) {
                const updatedCartItems = await response.json();
                
                const localCartFormat = updatedCartItems.map(item => ({
                    uuid: item.productUuid,
                    quantity: item.quantity
                }));
    
                localStorage.setItem('unlockitCart', JSON.stringify(localCartFormat));
    
                alert('Artikel wurden zum Warenkorb hinzugefügt.');
                window.location.href = 'cart.html'; 
            } else {
                const errorResult = await response.json();
                throw new Error(errorResult.message || 'Artikel konnten nicht nachbestellt werden.');
            }
        } catch (error) {
            console.error('Fehler beim Nachbestellen:', error);
            alert(`Fehler: ${error.message}`);
        }
    }

    if (limitSelect) {
        limitSelect.addEventListener('change', loadMyOrders);
    }

    loadMyOrders();
});