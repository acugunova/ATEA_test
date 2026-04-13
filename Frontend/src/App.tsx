import React from 'react';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { PRIVATE } from 'routes';
import { Suspense } from 'Suspense';

import './App.css';

export const App = () => {
  return (
    <div className="App">
      <header className="App-header">
        <BrowserRouter basename="/">
          <Routes>
            <Route path={PRIVATE.ORDER.path} element={<Suspense element={PRIVATE.ORDER.element} />} />
          </Routes>
        </BrowserRouter>
      </header>
    </div>
  );
};

export default App;
