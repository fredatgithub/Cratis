/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command } from '@aksio/cratis-applications-frontend/commands';

export class AddMicroservice extends Command {
    readonly route: string = '/api/compliance/microservices';

    microserviceId!: string;
    name!: string;
}
