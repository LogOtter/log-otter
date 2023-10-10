<script lang="ts">
import { defineComponent, type PropType } from "vue";
import type { Event } from "../services/EventStreamsService";
import EventBody from "../components/EventBody.vue";
import dayjs from "dayjs";
import { Tooltip } from "bootstrap";

export default defineComponent({
  components: {
    EventBody,
  },
  data() {
    return {
      expanded: false,
    };
  },
  props: {
    eventStreamName: { type: String, required: true },
    streamId: { type: String, required: true },
    event: { type: Object as PropType<Event>, required: true },
  },
  methods: {
    toggleExpanded() {
      this.expanded = !this.expanded;
    },
    formatTimestamp(timestamp: string) {
      return dayjs(timestamp).format("dddd, D MMMM YYYY HH:mm:ss");
    },
    formatTimestampFromNow(timestamp: string) {
      return dayjs(timestamp).fromNow();
    },
  },
  mounted() {
    new Tooltip(this.$refs.timeTooltip as Element);
  },
});
</script>

<template>
  <div class="card mb-1">
    <div class="card-body clickable" @click="toggleExpanded">
      <div class="row">
        <div class="col">
          <h6 class="card-title">{{ event.description }}</h6>
          <div class="card-subtitle text-body-secondary">
            {{ event.eventNumber }} &middot; {{ event.bodyType }} &middot;
            <span ref="timeTooltip" data-bs-toggle="tooltip" :data-bs-title="formatTimestamp(event.timestamp)">
              {{ formatTimestampFromNow(event.timestamp) }}
            </span>
          </div>
        </div>
        <div class="col-auto">
          <button type="button" class="btn btn-outline-secondary">
            <i
              :class="{
                'bi-chevron-up': expanded,
                'bi-chevron-down': !expanded,
              }"
            ></i>
          </button>
        </div>
      </div>
    </div>
    <div class="card-body pt-0" v-if="expanded">
      <event-body :event-stream-name="eventStreamName" :stream-id="streamId" :event-id="event.eventId"></event-body>
    </div>
  </div>
</template>

<style scoped>
.clickable {
  cursor: pointer;
}
</style>
