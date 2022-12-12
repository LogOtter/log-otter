<script lang="ts">
import { defineComponent } from "vue";
import SideBar from "./components/SideBar.vue";
import RootPage from "./pages/RootPage.vue";
import type EventStream from "./types/EventStream";

export default defineComponent({
  components: {
    SideBar,
    RootPage,
  },
  data() {
    return {
      eventStreams: [] as EventStream[],
    };
  },
  methods: {
    async fetchEventStreams() {
      this.eventStreams = [];

      const eventStreams = [];

      let url = `${import.meta.env.VITE_API_BASE_URL}api/`;

      do {
        const response = await fetch(url);
        const jsonResponse = await response.json();
        for (const definition of jsonResponse.definitions) {
          eventStreams.push(definition);
        }
        url = jsonResponse?._links?.next?.href;
      } while (url);

      this.eventStreams = eventStreams;
    },
  },
  mounted() {
    this.fetchEventStreams();
  },
});
</script>

<template>
  <side-bar :event-streams="eventStreams"></side-bar>
  <root-page :event-streams="eventStreams"></root-page>
</template>

<style scoped></style>
