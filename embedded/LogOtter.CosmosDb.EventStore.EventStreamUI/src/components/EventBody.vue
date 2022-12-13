<script lang="ts">
import { EventStreamsService } from "@/services/EventStreamsService";
import JsonViewer from "vue-json-viewer";

const eventStreamsService = new EventStreamsService();

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

      this.eventBody = await eventStreamsService.getEventBody(
        this.eventStreamName,
        this.streamId,
        this.eventId
      );

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
    <div v-if="loading" class="card p-3 placeholder-glow">
      <span class="placeholder col-3 mb-2"></span>
      <span class="placeholder col-4 mb-2"></span>
      <span class="placeholder col-2 mb-2"></span>
      <span class="placeholder col-3"></span>
    </div>
    <div v-if="!loading" class="card">
      <json-viewer :value="eventBody" :expand-depth="3" copyable></json-viewer>
    </div>
  </div>
</template>

<style scoped>
.card {
  max-height: 500px;
  overflow-y: auto;
}
</style>
