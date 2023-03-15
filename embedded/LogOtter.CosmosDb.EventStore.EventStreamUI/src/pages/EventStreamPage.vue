<script lang="ts">
import { defineComponent } from "vue";
import StreamIdSearchPanel from "../components/StreamIdSearchPanel.vue";

export default defineComponent({
  components: {
    StreamIdSearchPanel,
  },
  methods: {
    search(streamId: string) {
      if (this.serviceName) {
        window.location.hash = `#/service/${encodeURIComponent(this.serviceName)}/${encodeURIComponent(this.eventStreamName)}/${encodeURIComponent(
          streamId
        )}`;
      } else {
        window.location.hash = `#/${encodeURIComponent(this.eventStreamName)}/${encodeURIComponent(streamId)}`;
      }
    },
  },
  props: {
    eventStreamName: {
      type: String,
      required: true,
    },
    serviceName: {
      type: String,
      required: false,
    },
  },
});
</script>

<template>
  <div class="m-3">
    <h1 class="display-5 fw-bold mb-4 sidebar-margin">{{ eventStreamName }}</h1>
    <stream-id-search-panel @search="search"></stream-id-search-panel>
  </div>
</template>
