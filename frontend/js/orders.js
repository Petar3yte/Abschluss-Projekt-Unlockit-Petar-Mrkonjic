document.addEventListener('DOMContentLoaded', function () {
    const token = localStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    const ordersTableBody = document.getElementById('orders-table-body');
    const getStatusBadge = (status) => {
        switch (status) {
            case 'Ausstehend':
                return 'bg-warning text-dark';
            case 'in_Bearbeitung':
                return 'bg-info text-dark';
            case 'Versendet':
                return 'bg-success';
            case 'Storniert':
                return 'bg-danger';
            default:
                return 'bg-secondary';
        }
    };

    const fetchAndDisplayOrders = async () => {
        try {
            const response = await fetch('https://localhost:7007/api/Orders/all', {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.status === 401 || response.status === 403) {
                localStorage.removeItem('jwtToken');
                window.location.href = 'login.html';
                return;
            }

            if (!response.ok) {
                throw new Error(`HTTP-Fehler! Status: ${response.status}`);
            }

            const orders = await response.json();
            ordersTableBody.innerHTML = ''; 

            if (orders.length === 0) {
                ordersTableBody.innerHTML = '<tr><td colspan="6" class="text-center">Keine Bestellungen gefunden.</td></tr>';
                return;
            }

            orders.forEach(order => {
                const row = document.createElement('tr');
                
                const formattedDate = new Date(order.orderDate).toLocaleDateString('de-DE', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric'
                });

                const formattedAmount = new Intl.NumberFormat('de-DE', {
                    style: 'currency',
                    currency: 'EUR'
                }).format(order.totalAmount);

                row.innerHTML = `
                    <td><small>${order.orderUUID}</small></td>
                    <td>${order.customerName}</td>
                    <td>${formattedDate}</td>
                    <td><span class="badge ${getStatusBadge(order.orderStatus)}">${order.orderStatus.replace('_', ' ')}</span></td>
                    <td class="text-end">${formattedAmount}</td>
                    <td class="text-center">
                        <a href="order-detail.html?uuid=${order.orderUUID}" class="btn btn-sm btn-primary">Details</a>
                    </td>
                `;
                ordersTableBody.appendChild(row);
            });

        } catch (error) {
            console.error('Fehler beim Abrufen der Bestellungen:', error);
            ordersTableBody.innerHTML = '<tr><td colspan="6" class="text-center text-danger">Fehler beim Laden der Bestellungen.</td></tr>';
        }
    };

    fetchAndDisplayOrders();
});