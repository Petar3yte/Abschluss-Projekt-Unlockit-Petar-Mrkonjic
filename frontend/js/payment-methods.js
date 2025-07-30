document.addEventListener('DOMContentLoaded', function () {
    const token = localStorage.getItem('authToken');
    const tableBody = document.getElementById('payment-methods-table-body');
    const addForm = document.getElementById('add-payment-method-form');
    const newMethodNameInput = document.getElementById('new-method-name');

    const API_URL = 'https://localhost:7007/api/paymentmethods';

    async function fetchPaymentMethods() {
        try {
            const response = await fetch(API_URL, {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error('Fehler beim Abrufen der Zahlungsmethoden.');
            
            const methods = await response.json();
            tableBody.innerHTML = ''; 

            methods.forEach(method => {
                const row = document.createElement('tr');
                
                const statusBadge = method.isEnabled 
                    ? `<span class="badge bg-success">Aktiv</span>` 
                    : `<span class="badge bg-secondary">Inaktiv</span>`;

                const actionButton = method.isEnabled
                    ? `<button class="btn btn-sm btn-warning toggle-status-btn" data-id="${method.paymentMethodId}">Deaktivieren</button>`
                    : `<button class="btn btn-sm btn-success toggle-status-btn" data-id="${method.paymentMethodId}">Aktivieren</button>`;

                row.innerHTML = `
                    <td>${method.name}</td>
                    <td>${statusBadge}</td>
                    <td>${actionButton}</td>
                `;
                tableBody.appendChild(row);
            });
        } catch (error) {
            console.error(error.message);
            tableBody.innerHTML = `<tr><td colspan="3" class="text-danger">${error.message}</td></tr>`;
        }
    }

    async function toggleStatus(methodId) {
        try {
            const response = await fetch(`${API_URL}/${methodId}/toggle`, {
                method: 'PUT',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error('Status konnte nicht geändert werden.');
            
            fetchPaymentMethods(); 
        } catch (error) {
            console.error(error.message);
            alert(error.message);
        }
    }
    
    async function createPaymentMethod(name) {
        try {
            const response = await fetch(API_URL, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ name: name })
            });
            if (!response.ok) {
                 const errorData = await response.json();
                 throw new Error(errorData.title || 'Zahlungsmethode konnte nicht erstellt werden.');
            }
            newMethodNameInput.value = ''; 
            fetchPaymentMethods(); 
        } catch (error) {
            console.error(error.message);
            alert(error.message);
        }
    }

    tableBody.addEventListener('click', (e) => {
        if (e.target.classList.contains('toggle-status-btn')) {
            const methodId = e.target.dataset.id;
            toggleStatus(methodId);
        }
    });

    addForm.addEventListener('submit', (e) => {
        e.preventDefault();
        const methodName = newMethodNameInput.value.trim();
        if (methodName) {
            createPaymentMethod(methodName);
        }
    });

    fetchPaymentMethods();
});