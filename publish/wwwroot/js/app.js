window.checkIsAdmin = (token) => {
    if (!token) return false;
    
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
        return role === 'Admin';
    } catch {
        return false;
    }
};

window.authApi = {
    login: async (username, password) => {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, password })
        });

        if (!response.ok) {
            return { success: false, message: 'Username atau password salah' };
        }

        const data = await response.json();
        localStorage.setItem('authToken', data.token);

        return { success: true, token: data.token };
    },
    redirect: (url) => {
        window.location.assign(url);
    },
    logout: async () => {
        await fetch('/api/auth/logout', {
            method: 'POST',
            credentials: 'same-origin'
        });

        localStorage.removeItem('authToken');
    }
};

window.erpToggleSidebar = (open) => {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebar-overlay');
    if (open) {
        sidebar.classList.add('open');
        overlay.classList.add('open');
    } else {
        sidebar.classList.remove('open');
        overlay.classList.remove('open');
    }
};
