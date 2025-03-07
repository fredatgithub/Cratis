// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { default as styles } from './App.module.scss';
import { Navigation } from './Navigation';
import { Home } from './Home';
import { Ledger } from './Accounts/Debit/Ledger';
import { DebitAccounts } from './Accounts/Debit/DebitAccounts';
import { AccountHolders } from './AccountHolders/AccountHolders';
import { IdentityProvider } from '@aksio/cratis-applications-frontend/identity';

export const App = () => {
    return (
        <IdentityProvider>
            <Router>
                <div className={styles.appContainer}>
                    <div className={styles.navigationBar}>
                        <Navigation />
                    </div>
                    <div style={{ width: '100%' }}>
                        <Routes>
                            <Route path="/" element={<Home />} />
                            <Route path="/accounts/debit" element={<DebitAccounts />} />
                            <Route path="/accounts/debit/:id/ledger" element={<Ledger />} />
                            <Route path="/integration" element={<AccountHolders />} />
                        </Routes>
                    </div>
                </div>
            </Router>
        </IdentityProvider>
    );
};
