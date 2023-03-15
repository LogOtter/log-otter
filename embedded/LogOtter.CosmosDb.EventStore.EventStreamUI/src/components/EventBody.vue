<script lang="ts">
import type { EventStreamsService } from "@/services/EventStreamsService";
import { inject } from "vue";
import JsonViewer from "vue-json-viewer";

export default {
  setup() {
    return { eventStreamsService: inject<EventStreamsService>("eventStreamsService")! };
  },
  components: {
    JsonViewer,
  },
  data() {
    return {
      eventBody: {} as any,
      loading: false,
      error: undefined as any,
    };
  },
  mounted() {
    this.fetchEventBody();
  },
  methods: {
    async fetchEventBody() {
      this.loading = true;

      try {
        this.eventBody = await this.eventStreamsService.getEventBody(this.serviceName, this.eventStreamName, this.streamId, this.eventId);
      } catch (e) {
        this.error = e;
      }

      this.loading = false;
    },
  },
  props: {
    serviceName: { type: String, required: false },
    eventStreamName: { type: String, required: true },
    streamId: { type: String, required: true },
    eventId: { type: String, required: true },
  },
};
</script>

<template>
  <div>
    <div v-if="loading" class="card p-3 placeholder-glow">
      <span class="placeholder col-3 mb-2"></span>
      <span class="placeholder col-4 mb-2"></span>
      <span class="placeholder col-2 mb-2"></span>
      <span class="placeholder col-3"></span>
    </div>
    <div v-if="!loading && !error" class="card">
      <json-viewer :value="eventBody" :expand-depth="3" copyable></json-viewer>
    </div>
    <div v-if="!loading && error" class="card mb-1 p-3 text-bg-danger">
      <span><i class="bi-exclamation-square me-2"></i> <strong>Error</strong> - Could not load event body</span>
    </div>
  </div>
</template>

<style scoped>
.card {
  max-height: 500px;
  overflow-y: auto;
}
</style>
