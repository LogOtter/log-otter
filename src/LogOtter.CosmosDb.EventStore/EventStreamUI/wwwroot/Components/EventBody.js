export default {
    data() {
      return {
          eventBody: null,
          loading: false
      }  
    },
    mounted() {
      this.fetchEventBody();
    },
    methods: {
      async fetchEventBody() {
          this.loading = true;

          const url = `api/${this.eventStreamName}/${encodeURIComponent(this.streamId)}/events/${this.eventId}/body`;
          
          const response = await fetch(url);
          this.eventBody = JSON.stringify(await response.json(), null, '  ');
          
          this.loading = false;
      }  
    },
    props: {
        eventStreamName: String,
        streamId: String,
        eventId: String
    },
    template: `
    <div>
        <div v-if="loading">Loading...</div>
        <div v-if="!loading" class="card text-bg-light" style="max-height: 500px; overflow-y: auto;">
            <pre class="m-2 p-2"><code>{{ eventBody }}</code></pre>
        </div>
    </div>
    `
}