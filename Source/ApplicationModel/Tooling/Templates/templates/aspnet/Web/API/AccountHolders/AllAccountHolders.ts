/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { AccountHolder } from './../../../Web/API/accountholders/AccountHolder';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accountholders');

export class AllAccountHolders extends QueryFor<AccountHolder[]> {
    readonly route: string = '/api/accountholders';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: AccountHolder[] = [];

    constructor() {
        super(AccountHolder, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<AccountHolder[]>, PerformQuery] {
        return useQuery<AccountHolder[], AllAccountHolders>(AllAccountHolders);
    }
}
