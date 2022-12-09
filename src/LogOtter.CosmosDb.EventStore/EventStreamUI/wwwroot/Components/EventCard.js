import EventBody from './EventBody.js';

export default {
    components: {
      EventBody  
    },
    data() {
        return {
            expanded: false
        };
    },
    props: {
        eventStreamName: String,
        streamId: String,
        event: Object
    },
    methods: {
        toggleExpanded() {
            this.expanded = !this.expanded  
        },
        formatTimestamp(timestamp) {
            return dayjs(timestamp).format('dddd, D MMMM HH:mm:ss');
        },
        formatTimestampFromNow(timestamp) {
            return dayjs(timestamp).fromNow();
        }
    },
    mounted() {
        new bootstrap.Tooltip(this.$refs.timeTooltip);
    },
    template: `
    <div class="card mb-1">
        <div class="card-body">
            <div class="row">
                <div class="col">
                    <h6 class="card-title">{{ event.description }}</h6>
                    <div class="card-subtitle text-muted">
                        {{ event.eventNumber }} &middot; 
                        {{ event.bodyType }} &middot;
                        <span ref="timeTooltip" data-bs-toggle="tooltip" :data-bs-title="formatTimestamp(event.timestamp)">{{ formatTimestampFromNow(event.timestamp) }}</span>
                    </div>
                </div>
                <div class="col-auto">
                    <button type="button" class="btn btn-outline-secondary" @click="toggleExpanded">
                        <i :class="{ 'bi-chevron-up': expanded, 'bi-chevron-down': !expanded }"></i>
                    </button>
                </div>
            </div>
            <a href="javascript:void(0)" class="stretched-link" @click="toggleExpanded"></a>
            <div v-if="expanded" class="mt-4">
                <event-body :event-stream-name="eventStreamName" :stream-id="streamId" :event-id="event.eventId"></event-body>
            </div>
        </div>  
    </div>
    `
}