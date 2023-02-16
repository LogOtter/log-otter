import { createApp } from "vue";
import AppContainer from "./AppContainer.vue";
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap";
import "bootstrap-icons/font/bootstrap-icons.css";
import "./assets/main.css";
import * as dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";

dayjs.extend(relativeTime);

createApp(AppContainer).mount("#app");
