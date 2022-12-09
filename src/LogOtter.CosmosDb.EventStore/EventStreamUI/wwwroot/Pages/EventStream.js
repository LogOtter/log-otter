import StreamIdSearchPanel from "../Components/StreamIdSearchPanel.js";

export default {
    components: {
      StreamIdSearchPanel  
    },
    methods: {
        search(streamId) {
            window.location.hash = `#/${this.eventStreamName}/${encodeURIComponent(streamId)}`;
        }
    },
    props: {
        eventStreamName: String
    },
    template: `
    <div class="m-3">
        <h1 class="display-5 fw-bold mb-4">{{ eventStreamName }}</h1>
        <stream-id-search-panel @search="search"></stream-id-search-panel>
    </div>
    `
}