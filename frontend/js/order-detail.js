document.addEventListener('DOMContentLoaded', function () {
    const token = localStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    const urlParams = new URLSearchParams(window.location.search);
    const orderUuid = urlParams.get('uuid');

    if (!orderUuid) {
        document.getElementById('order-content').innerHTML = '<h1 class="text-danger">Fehler: Keine Bestell-ID angegeben.</h1>';
        return;
    }

    const loadingSpinner = document.getElementById('loading-spinner');
    const orderContent = document.getElementById('order-content');
    const statusSelect = document.getElementById('status-select');
    const saveStatusBtn = document.getElementById('save-status-btn');
    const statusFeedback = document.getElementById('status-feedback');

    const getStatusBadge = (status) => {
        const statuses = {
            'Ausstehend': 'bg-warning text-dark',
            'in_Bearbeitung': 'bg-info text-dark',
            'Versendet': 'bg-success',
            'Storniert': 'bg-danger'
        };
        return statuses[status] || 'bg-secondary';
    };

    const loadOrderDetails = async () => {
        loadingSpinner.style.display = 'block';
        orderContent.classList.add('d-none');

        try {
            const response = await fetch(`https://localhost:7007/api/Orders/${orderUuid}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (!response.ok) throw new Error(`Fehler ${response.status}`);
            
            const order = await response.json();

            document.getElementById('order-uuid').textContent = order.orderUUID;
            document.getElementById('customer-name').textContent = order.customerName;
            document.getElementById('customer-email').textContent = order.customerEmail;
            document.getElementById('order-date').textContent = new Date(order.orderDate).toLocaleDateString('de-DE');
            document.getElementById('total-amount').textContent = new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(order.totalAmount);
            
            const statusBadge = document.getElementById('current-status');
            statusBadge.textContent = order.orderStatus.replace('_', ' ');
            statusBadge.className = `badge ${getStatusBadge(order.orderStatus)}`;
            
            statusSelect.value = order.orderStatus;

            const itemsTable = document.getElementById('order-items-table');
            itemsTable.innerHTML = '';
            order.items.forEach(item => {
                const row = itemsTable.insertRow();
                row.innerHTML = `
                    <td>${item.productName}</td>
                    <td>${item.quantity}</td>
                    <td class="text-end">${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(item.unitPrice)}</td>
                    <td class="text-end">${new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(item.quantity * item.unitPrice)}</td>
                `;
            });

        } catch (error) {
            orderContent.innerHTML = `<h1 class="text-danger text-center">Fehler beim Laden der Bestelldetails.</h1><p class="text-center">${error.message}</p>`;
        } finally {
            loadingSpinner.style.display = 'none';
            orderContent.classList.remove('d-none');
        }
    };
    
    saveStatusBtn.addEventListener('click', async () => {
        const newStatus = statusSelect.value;
        statusFeedback.textContent = '';
        saveStatusBtn.disabled = true;

        try {
            const response = await fetch(`https://localhost:7007/api/Orders/${orderUuid}/status`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ newStatus: newStatus })
            });

            const result = await response.json();

            if (!response.ok) throw new Error(result.message || 'Unbekannter Fehler');

            statusFeedback.textContent = 'Status erfolgreich gespeichert!';
            statusFeedback.className = 'mt-2 text-success';
            loadOrderDetails();

        } catch (error) {
            statusFeedback.textContent = `Fehler: ${error.message}`;
            statusFeedback.className = 'mt-2 text-danger';
        } finally {
            saveStatusBtn.disabled = false;
        }
    });

    loadOrderDetails();
});