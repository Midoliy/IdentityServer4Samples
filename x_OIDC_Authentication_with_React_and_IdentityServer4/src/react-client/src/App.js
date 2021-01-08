// import logo from './logo.svg';
// import store from './store';
// import { useEffect } from 'react';

// import './App.css';

// function App() {
//   useEffect(() => {
//     loadUserFromStorage(store);
//   }, [])

//   return (
//     <div className="App">
//       <header className="App-header">
//         <img src={logo} className="App-logo" alt="logo" />
//         <p>
//           Edit <code>src/App.js</code> and save to reload.
//         </p>
//         <a
//           className="App-link"
//           href="https://reactjs.org"
//           target="_blank"
//           rel="noopener noreferrer"
//         >
//           Learn React
//         </a>
//       </header>
//     </div>
//   );
// }

// export default App;

import React, { useEffect } from 'react';
import { BrowserRouter as Router, Route, Switch } from 'react-router-dom'
import SigninOidc from './pages/signin-oidc'
import SignoutOidc from './pages/signout-oidc'
import Home from './pages/home'
import Login from './pages/login'
import { Provider } from 'react-redux';
import store from './store';
import userManager, { loadUserFromStorage } from './services/userService'
import AuthProvider from './utils/authProvider'
import PrivateRoute from './utils/protectedRoute'

function App() {

  useEffect(() => {
    // fetch current user from cookies
    loadUserFromStorage(store)
  }, [])

  return (
    <Provider store={store}>
      <AuthProvider userManager={userManager} store={store}>
        <Router>
          <Switch>
            <Route path="/login" component={Login} />
            <Route path="/signout-oidc" component={SignoutOidc} />
            <Route path="/signin-oidc" component={SigninOidc} />
            <PrivateRoute exact path="/" component={Home} />
          </Switch>
        </Router>
      </AuthProvider>
    </Provider>
  );
}

export default App;