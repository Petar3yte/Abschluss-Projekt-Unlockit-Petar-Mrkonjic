document.addEventListener('DOMContentLoaded', function () {
    const token = localStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    const urlParams = new URLSearchParams(window.location.search);
    const userUuid = urlParams.get('uuid');

    if (!userUuid) {
        document.getElementById('user-content').innerHTML = '<h1 class="text-danger">Fehler: Keine Benutzer-ID angegeben.</h1>';
        return;
    }

    let currentUserData = null;

    const loadingSpinner = document.getElementById('loading-spinner');
    const userContent = document.getElementById('user-content');
    const orderCountSelect = document.getElementById('order-count-select');

    const editBtn = document.getElementById('edit-user-btn');
    const saveBtn = document.getElementById('save-user-btn');
    const cancelBtn = document.getElementById('cancel-edit-btn');
    const displayModeDiv = document.getElementById('display-mode');
    const editModeDiv = document.getElementById('edit-mode');
    const editActionsDiv = document.getElementById('edit-actions');
    const editFeedback = document.getElementById('edit-feedback');
    
    const uuidDisplay = document.getElementById('user-uuid');
    const usernameDisplay = document.getElementById('user-username-display');
    const fullnameDisplay = document.getElementById('user-fullname-display');
    const emailDisplay = document.getElementById('user-email-display');
    const roleDisplay = document.getElementById('user-role-display');

    const usernameInput = document.getElementById('username-input');
    const emailInput = document.getElementById('email-input');
    const firstnameInput = document.getElementById('firstname-input');
    const lastnameInput = document.getElementById('lastname-input');
    const roleSelect = document.getElementById('role-select');
    const passwordInput = document.getElementById('password-input');


    const getStatusBadge = (status) => {
        const statuses = {
            'Ausstehend': 'bg-warning text-dark',
            'in_Bearbeitung': 'bg-info text-dark',
            'Versendet': 'bg-success',
            'Storniert': 'bg-danger'
        };
        return statuses[status] || 'bg-secondary';
    };

    const getRoleBadge = (role) => {
        const roles = {
            'Admin': 'bg-danger',
            'Mitarbeiter': 'bg-info text-dark',
            'Kunde': 'bg-secondary'
        };
        return roles[role] || 'bg-light text-dark';
    }


    const loadUserDetails = async (orderCount = 5) => {
        loadingSpinner.style.display = 'block';
        userContent.classList.add('d-none');
        try {
            const response = await fetch(`https://localhost:7007/api/Users/${userUuid}?orderCount=${orderCount}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (!response.ok) {
                throw new Error(`Benutzer nicht gefunden oder Zugriffsfehler (Status: ${response.status})`);
            }
            
            currentUserData = await response.json();
            displayUserData();

        } catch (error) {
            userContent.innerHTML = `<h1 class="text-danger text-center">Fehler beim Laden der Benutzerdetails.</h1><p class="text-center">${error.message}</p>`;
        } finally {
            loadingSpinner.style.display = 'none';
            userContent.classList.remove('d-none');
        }
    };
    
    function displayUserData() {
        uuidDisplay.textContent = currentUserData.userUUID;
        usernameDisplay.textContent = currentUserData.userName;
        const fullName = (currentUserData.firstName || currentUserData.lastName) ? `${currentUserData.firstName || ''} ${currentUserData.lastName || ''}`.trim() : 'N/A';
        fullnameDisplay.textContent = fullName;
        emailDisplay.textContent = currentUserData.email;
        roleDisplay.textContent = currentUserData.role;
        roleDisplay.className = `badge ${getRoleBadge(currentUserData.role)}`;
        
        const ordersSection = document.getElementById('recent-orders-section');
        const ordersTableBody = document.getElementById('orders-table-body');
        document.getElementById('orders-header').textContent = `Letzte ${orderCountSelect.value} Bestellungen`;

        if (currentUserData.recentOrders && currentUserData.recentOrders.length > 0) {
            ordersSection.classList.remove('d-none');
            ordersTableBody.innerHTML = '';
            
            currentUserData.recentOrders.forEach(order => {
                const row = ordersTableBody.insertRow();
                const formattedDate = new Date(order.orderDate).toLocaleDateString('de-DE');
                const formattedAmount = new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' }).format(order.totalAmount);

                row.innerHTML = `
                    <td><small>${order.orderUUID}</small></td>
                    <td>${formattedDate}</td>
                    <td><span class="badge ${getStatusBadge(order.orderStatus)}">${order.orderStatus.replace('_', ' ')}</span></td>
                    <td class="text-end">${formattedAmount}</td>
                    <td class="text-center">
                        <a href="order-details.html?uuid=${order.orderUUID}" class="btn btn-sm btn-primary">Details</a>
                    </td>
                `;
            });
        } else {
            ordersSection.classList.add('d-none');
        }
    }

    function toggleEditMode(isEditing) {
        if (isEditing) {
            displayModeDiv.classList.add('d-none');
            editModeDiv.classList.remove('d-none');
            editActionsDiv.classList.remove('d-none');
            editBtn.classList.add('d-none');
            
            usernameInput.value = currentUserData.userName;
            emailInput.value = currentUserData.email;
            firstnameInput.value = currentUserData.firstName || '';
            lastnameInput.value = currentUserData.lastName || '';
            roleSelect.value = currentUserData.role;
            passwordInput.value = '';
            editFeedback.textContent = '';
        } else {
            displayModeDiv.classList.remove('d-none');
            editModeDiv.classList.add('d-none');
            editActionsDiv.classList.add('d-none');
            editBtn.classList.remove('d-none');
        }
    }

    editBtn.addEventListener('click', () => toggleEditMode(true));
    cancelBtn.addEventListener('click', () => toggleEditMode(false));

    saveBtn.addEventListener('click', async () => {
        const updatedData = {
            userName: usernameInput.value,
            email: emailInput.value,
            firstName: firstnameInput.value,
            lastName: lastnameInput.value,
            role: roleSelect.value,
            password: passwordInput.value || null
        };

        saveBtn.disabled = true;
        editFeedback.textContent = 'Speichere...';
        editFeedback.className = 'mt-2 d-inline-block ms-2 text-info';

        try {
            const response = await fetch(`https://localhost:7007/api/Users/${userUuid}`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(updatedData)
            });

            if (!response.ok) {
                const errorResult = await response.text();
                throw new Error(errorResult || 'Fehler beim Speichern.');
            }
            
            editFeedback.textContent = 'Erfolgreich gespeichert!';
            editFeedback.className = 'mt-2 d-inline-block ms-2 text-success';
            
            await loadUserDetails(orderCountSelect.value);

            setTimeout(() => {
                toggleEditMode(false);
            }, 1500);

        } catch (error) {
            editFeedback.textContent = error.message;
            editFeedback.className = 'mt-2 d-inline-block ms-2 text-danger';
        } finally {
            saveBtn.disabled = false;
        }
    });

    orderCountSelect.addEventListener('change', () => {
        const selectedCount = orderCountSelect.value;
        loadUserDetails(selectedCount);
    });

    loadUserDetails(orderCountSelect.value);
});