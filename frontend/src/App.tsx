import { useState } from 'react'

function App() {
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'))
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  const login = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    try {
      const res = await fetch('http://localhost:8080/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })

      if (!res.ok) {
        setError('Invalid credentials')
        return
      }

      const data = await res.json()
      localStorage.setItem('token', data.token)
      setToken(data.token)
    } catch {
      setError('Could not connect to server')
    }
  }

  const logout = () => {
    localStorage.removeItem('token')
    setToken(null)
  }

  if (token) {
    return (
      <div>
        <p>Logged in</p>
        <button onClick={logout}>Logout</button>
      </div>
    )
  }

  return (
    <form onSubmit={login}>
      <h1>Login</h1>
      <input
        type="email"
        placeholder="Email"
        value={email}
        onChange={e => setEmail(e.target.value)}
        required
      />
      <input
        type="password"
        placeholder="Password"
        value={password}
        onChange={e => setPassword(e.target.value)}
        required
      />
      {error && <p>{error}</p>}
      <button type="submit">Login</button>
    </form>
  )
}

export default App
