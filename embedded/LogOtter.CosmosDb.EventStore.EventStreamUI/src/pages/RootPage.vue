<script lang="ts">
import type EventStream from "@/types/EventStream";
import { defineComponent, type PropType } from "vue";
import EventStreamsListPage from "./EventStreamsListPage.vue";
import EventStreamPage from "./EventStreamPage.vue";
import EventListPage from "./EventListPage.vue";
import NotFoundPage from "./NotFoundPage.vue";

export default defineComponent({
  data() {
    return { currentHash: window.location.hash };
  },
  props: {
    eventStreams: Array as PropType<EventStream[]>,
  },
  computed: {
    currentPath() {
      return this.currentHash.slice(1) || "/";
    },
    currentView() {
      if (this.currentPath === "/") {
        return {
          view: EventStreamsListPage,
          properties: { eventStreams: this.eventStreams },
        };
      }

      const eventStreamRegex = /^\/([^/]+)$/gm.exec(this.currentPath);
      if (eventStreamRegex !== null) {
        return {
          view: EventStreamPage,
          properties: { eventStreamName: eventStreamRegex[1] },
        };
      }

      const eventListRegex = /^\/([^/]+)\/([^/]+)$/gm.exec(this.currentPath);
      if (eventListRegex !== null) {
        return {
          view: EventListPage,
          properties: {
            eventStreamName: eventListRegex[1],
            streamId: decodeURIComponent(eventListRegex[2]),
          },
        };
      }

      return { view: NotFoundPage, properties: {} };
    },
  },
  mounted() {
    window.addEventListener("hashchange", () => {
      this.currentHash = window.location.hash;
    });
  },
});
</script>

<template>
  <div class="flex-grow-1" style="overflow-y: auto">
    <div class="container mx-0">
      <component :is="currentView.view" v-bind="currentView.properties" />
    </div>
  </div>
</template>
