import {define} from 'preactement';
import {App} from './app.tsx';

define(
    'csp-widget',
    () => App,
    {
        attributes: ['api-key', 'api-url']
    }
);