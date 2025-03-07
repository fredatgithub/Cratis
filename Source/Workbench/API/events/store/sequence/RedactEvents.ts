/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/redact-events');

export interface IRedactEvents {
    microserviceId?: string;
    eventSequenceId?: string;
    tenantId?: string;
    eventSourceId?: string;
    reason?: string;
    eventTypes?: string[];
}

export class RedactEventsValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        microserviceId: new Validator(),
        eventSequenceId: new Validator(),
        tenantId: new Validator(),
        eventSourceId: new Validator(),
        reason: new Validator(),
        eventTypes: new Validator(),
    };
}

export class RedactEvents extends Command<IRedactEvents> implements IRedactEvents {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/sequence/{{eventSequenceId}}/redact-events';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RedactEventsValidator();

    private _microserviceId!: string;
    private _eventSequenceId!: string;
    private _tenantId!: string;
    private _eventSourceId!: string;
    private _reason!: string;
    private _eventTypes!: string[];

    constructor() {
        super(Object, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'eventSequenceId',
            'tenantId',
        ];
    }

    get properties(): string[] {
        return [
            'microserviceId',
            'eventSequenceId',
            'tenantId',
            'eventSourceId',
            'reason',
            'eventTypes',
        ];
    }

    get microserviceId(): string {
        return this._microserviceId;
    }

    set microserviceId(value: string) {
        this._microserviceId = value;
        this.propertyChanged('microserviceId');
    }
    get eventSequenceId(): string {
        return this._eventSequenceId;
    }

    set eventSequenceId(value: string) {
        this._eventSequenceId = value;
        this.propertyChanged('eventSequenceId');
    }
    get tenantId(): string {
        return this._tenantId;
    }

    set tenantId(value: string) {
        this._tenantId = value;
        this.propertyChanged('tenantId');
    }
    get eventSourceId(): string {
        return this._eventSourceId;
    }

    set eventSourceId(value: string) {
        this._eventSourceId = value;
        this.propertyChanged('eventSourceId');
    }
    get reason(): string {
        return this._reason;
    }

    set reason(value: string) {
        this._reason = value;
        this.propertyChanged('reason');
    }
    get eventTypes(): string[] {
        return this._eventTypes;
    }

    set eventTypes(value: string[]) {
        this._eventTypes = value;
        this.propertyChanged('eventTypes');
    }

    static use(initialValues?: IRedactEvents): [RedactEvents, SetCommandValues<IRedactEvents>, ClearCommandValues] {
        return useCommand<RedactEvents, IRedactEvents>(RedactEvents, initialValues);
    }
}
