import EventStreamsList from "./EventStreamsList.js";
import EventStream from "./EventStream.js";
import EventList from "./EventList.js";
import NotFound from "./NotFound.js";

export default {
    data() {
        return {currentHash: window.location.hash}
    },
    props: {
        eventStreams: Array
    },
    computed: {
        currentPath() {
            return this.currentHash.slice(1) || '/';
        },
        currentView() {
            if (this.currentPath === '/') {
                return {view: EventStreamsList, properties: {eventStreams: this.eventStreams}};
            }

            const eventStreamRegex = /^\/([^/]+)$/gm.exec(this.currentPath);
            if (eventStreamRegex !== null) {
                return {view: EventStream, properties: {eventStreamName: eventStreamRegex[1]}};
            }

            const eventListRegex = /^\/([^/]+)\/([^/]+)$/gm.exec(this.currentPath);
            if (eventListRegex !== null) {
                return {view: EventList, properties: {eventStreamName: eventListRegex[1], streamId: decodeURIComponent(eventListRegex[2])}};
            }

            return {view: NotFound};
        }
    },
    mounted() {
        window.addEventListener('hashchange', () => {
            this.currentHash = window.location.hash
        });
    },
    template: `
    <div class="flex-grow-1" style="overflow-y: auto">
        <div class="container">
            <component :is="currentView.view" v-bind="currentView.properties" />
        </div>
    </div>
    `
}