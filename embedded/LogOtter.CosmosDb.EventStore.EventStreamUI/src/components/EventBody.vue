<script lang="ts">
import JsonViewer from "vue-json-viewer";

export default {
  components: {
    JsonViewer,
  },
  data() {
    return {
      eventBody: {} as any,
      loading: false,
    };
  },
  mounted() {
    this.fetchEventBody();
  },
  methods: {
    async fetchEventBody() {
      this.loading = true;

      const url = `${import.meta.env.VITE_API_BASE_URL}api/${
        this.eventStreamName
      }/${encodeURIComponent(this.streamId)}/events/${this.eventId}/body`;

      const response = await fetch(url);
      this.eventBody = await response.json();

      this.loading = false;
    },
  },
  props: {
    eventStreamName: { type: String, required: true },
    streamId: { type: String, required: true },
    eventId: { type: String, required: true },
  },
};
</script>

<template>
  <div>
    <div v-if="loading">Loading...</div>
    <div
      v-if="!loading"
      class="card"
      style="max-height: 500px; overflow-y: auto"
    >
      <json-viewer :value="eventBody" :expand-depth="3" copyable></json-viewer>
    </div>
  </div>
</template>
