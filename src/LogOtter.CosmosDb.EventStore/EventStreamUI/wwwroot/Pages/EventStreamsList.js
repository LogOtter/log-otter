export default {
    props: {
        eventStreams: Array
    },
    template: `
    <div class="m-3">
        <h1 class="display-5 fw-bold mb-4">Event Streams</h1>
        <div class="card mb-2" v-for="eventStream in eventStreams" :key="eventStream.name">
            <div class="card-body">
                <h5 class="card-title">{{ eventStream.name }}</h5>
                <div class="card-subtitle text-muted">{{ eventStream.typeName }}</div>
                <a :href="'#/' + eventStream.name" class="stretched-link"></a>
            </div>
        </div>
    </div>
    `
}