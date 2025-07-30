document.addEventListener('DOMContentLoaded', function () {
    const token = localStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    const usersTableBody = document.getElementById('users-table-body');

    const fetchAndDisplayUsers = async () => {
        try {
            const response = await fetch('https://localhost:7007/api/Users', {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (!response.ok) {
                if (response.status === 401 || response.status === 403) {
                    window.location.href = 'login.html';
                }
                throw new Error(`HTTP-Fehler! Status: ${response.status}`);
            }

            const users = await response.json();
            usersTableBody.innerHTML = ''; 

            if (users.length === 0) {
                usersTableBody.innerHTML = '<tr><td colspan="5" class="text-center">Keine Benutzer gefunden.</td></tr>';
                return;
            }

            users.forEach(user => {
                const row = document.createElement('tr');
                const fullName = (user.firstName || user.lastName) ? `${user.firstName || ''} ${user.lastName || ''}`.trim() : 'N/A';
                
                row.innerHTML = `
                    <td>${user.userName}</td>
                    <td>${fullName}</td>
                    <td>${user.email}</td>
                    <td>${user.role}</td>
                    <td class="text-center">
                        <a href="user-detail.html?uuid=${user.userUUID}" class="btn btn-sm btn-primary">Details</a>
                        <button class="btn btn-sm btn-danger" onclick="deleteUser('${user.userUUID}')">Löschen</button>
                    </td>
                `;
                usersTableBody.appendChild(row);
            });

        } catch (error) {
            console.error('Fehler beim Abrufen der Benutzer:', error);
            usersTableBody.innerHTML = '<tr><td colspan="5" class="text-center text-danger">Fehler beim Laden der Benutzer.</td></tr>';
        }
    };

    fetchAndDisplayUsers();
});

async function deleteUser(userUuid) {
    if (!confirm('Sind Sie sicher, dass Sie diesen Benutzer endgültig löschen möchten?')) {
        return;
    }

    const token = localStorage.getItem('authToken');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    try {
        const response = await fetch(`https://localhost:7007/api/Users/${userUuid}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.ok) {
            alert('Benutzer erfolgreich gelöscht.');
            window.location.reload(); 
        } else {
            let errorMessage = `Fehler beim Löschen des Benutzers (Status: ${response.status})`;
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || errorMessage;
            } catch (e) {
            }
            throw new Error(errorMessage);
        }
    } catch (error) {
        console.error('Fehler:', error);
        alert(`Ein Fehler ist aufgetreten: ${error.message}`);
    }
}