import StreamIdSearchPanel from "../Components/StreamIdSearchPanel.js";
import EventCard from "../Components/EventCard.js";
export default {
    components: {
        StreamIdSearchPanel,
        EventCard
    },
    data() {
        return {
            events: []
        };
    },
    methods: {
        search(streamId) {
            window.location.hash = `#/${this.eventStreamName}/${encodeURIComponent(streamId)}`;
        },
        async fetchEvents() {
            const events = [];

            let url = `api/${this.eventStreamName}/${encodeURIComponent(this.streamId)}/events`;

            do {
                const response = await fetch(url);
                const jsonResponse = await response.json();
                for (const event of jsonResponse.events) {
                    events.push(event);
                }
                url = jsonResponse?._links?.next?.href;
            } while (!!url);

            this.events = events;
        }
    },
    props: {
        eventStreamName: String,
        streamId: String
    },
    mounted() {
        this.fetchEvents();
    },
    template: `
    <div class="m-3">
        <h1 class="display-5 fw-bold mb-4">{{ eventStreamName }}</h1>
        <stream-id-search-panel @search="search" :starting-stream-id="streamId"></stream-id-search-panel>
        <div>
            <event-card :event-stream-name="eventStreamName" :stream-id="streamId" :event="event" v-for="event in events" :key="event.id"></event-card>
        </div>
    </div>
    `
}